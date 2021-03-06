﻿using System;
using System.Collections.Generic;
using System.Linq;
using OptimaJet.Workflow.Core.Model;
using OptimaJet.Workflow.Core.Runtime;
using Microsoft.Extensions.DependencyInjection;
using OptimaJet.DWKit.StarterApplication.Repositories;
using OptimaJet.DWKit.StarterApplication.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using OptimaJet.DWKit.StarterApplication.Services.BuildEngine;
using Hangfire;
using OptimaJet.DWKit.Application;
using OptimaJet.DWKit.Core.Model;
using OptimaJet.DWKit.Core;

namespace OptimaJet.DWKit.StarterApplication.Services.Workflow
{
    public class WorkflowProductActionProvider : IWorkflowActionProvider
    {
        private readonly Dictionary<string, Action<ProcessInstance, WorkflowRuntime, string>> _actions = new Dictionary<string, Action<ProcessInstance, WorkflowRuntime, string>>();

        private readonly Dictionary<string, Func<ProcessInstance, WorkflowRuntime, string, CancellationToken, Task>> _asyncActions =
            new Dictionary<string, Func<ProcessInstance, WorkflowRuntime, string, CancellationToken, Task>>();

        private readonly Dictionary<string, Func<ProcessInstance, WorkflowRuntime, string, bool>> _conditions =
            new Dictionary<string, Func<ProcessInstance, WorkflowRuntime, string, bool>>();

        private readonly Dictionary<string, Func<ProcessInstance, WorkflowRuntime, string, CancellationToken, Task<bool>>> _asyncConditions =
            new Dictionary<string, Func<ProcessInstance, WorkflowRuntime, string, CancellationToken, Task<bool>>>();

        public IServiceProvider ServiceProvider { get; }
        public IBackgroundJobClient BackgroundJobClient { get; }

        public WorkflowProductActionProvider(IServiceProvider serviceProvider, IBackgroundJobClient backgroundJobClient)
        {
            ServiceProvider = serviceProvider;
            BackgroundJobClient = backgroundJobClient;

            //Register your actions in _actions and _asyncActions dictionaries
            _asyncActions.Add("WriteProductTransition", WriteProductTransitionAsync);
            _asyncActions.Add("UpdateProductTransition", UpdateProductTransitionAsync);
            _asyncActions.Add("SendOwnerNotification", SendOwnerNotificationAsync);
            _asyncActions.Add("BuildEngine_CreateProduct", BuildEngineCreateProductAsync);
            _asyncActions.Add("BuildEngine_BuildProduct", BuildEngineBuildProductAsync);

            //Register your conditions in _conditions and _asyncConditions dictionaries
            //_asyncConditions.Add("CheckBigBossMustSign", CheckBigBossMustSignAsync); 
            _asyncConditions.Add("BuildEngine_ProductCreated", BuildEngineProductCreated);
            _asyncConditions.Add("BuildEngine_BuildCompleted", BuildEngineBuildCompleted);
            _asyncConditions.Add("BuildEngine_BuildFailed", BuildEngineBuildFailed);
        }

        //
        // Conditions
        //
        private async Task<bool> BuildEngineProductCreated(ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter, CancellationToken token)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var productRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Product, Guid>>();
                Product product = await GetProductForProcess(processInstance, productRepository);
                Log.Information($"BuildEngineProductCreated: workflowJobId={product.WorkflowJobId}, productId={product.Id}, projectName={product.Project.Name}");
                return product.WorkflowJobId != 0;
            }
        }

        private async Task<bool> BuildEngineBuildCompleted(ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter, CancellationToken token)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var productRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Product, Guid>>();
                Product product = await GetProductForProcess(processInstance, productRepository);
                Log.Information($"BuildEngineBuildCompleted: workflowJobId={product.WorkflowBuildId}, productId={product.Id}, projectName={product.Project.Name}");

                bool buildCompleted = false;
                if (product.WorkflowBuildId != 0)
                {
                    var service = scope.ServiceProvider.GetRequiredService<BuildEngineBuildService>();
                    var status = await service.GetStatusAsync(product.Id);
                    buildCompleted = (status == BuildEngineStatus.Success);
                }
                return buildCompleted;
            }
        }

        private async Task<bool> BuildEngineBuildFailed(ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter, CancellationToken token)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var productRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Product, Guid>>();
                Product product = await GetProductForProcess(processInstance, productRepository);
                Log.Information($"BuildEngineProductCreated: workflowJobId={product.WorkflowBuildId}, productId={product.Id}, projectName={product.Project.Name}");

                bool buildFailed = false;
                if (product.WorkflowBuildId != 0)
                {
                    var service = scope.ServiceProvider.GetRequiredService<BuildEngineBuildService>();
                    var status = await service.GetStatusAsync(product.Id);
                    buildFailed = (status == BuildEngineStatus.Failure);
                }
                return buildFailed;
            }
        }


        //
        // Actions
        //
        private async Task BuildEngineCreateProductAsync(ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter, CancellationToken token)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var productRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Product, Guid>>();
                Product product = await GetProductForProcess(processInstance, productRepository);
                if (product.WorkflowJobId == 0) 
                {
                    BackgroundJobClient.Enqueue<BuildEngineProductService>(service => service.ManageProduct(product.Id));
                    Log.Information($"BuildEngineCreateProduct: productId={product.Id}, projectName={product.Project.Name}");
                }
                else
                {
                    Log.Warning($"Product \"{product.Id}\" already has a BuildEngine Product \"{product.WorkflowJobId}\"");
                }
            }
        }

        private async Task BuildEngineBuildProductAsync(ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter, CancellationToken token)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var productRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Product, Guid>>();
                Product product = await GetProductForProcess(processInstance, productRepository);
                if (product.WorkflowJobId != 0)
                {
                    product.WorkflowBuildId = 0;
                    await productRepository.UpdateAsync(product);
                    BackgroundJobClient.Enqueue<BuildEngineBuildService>(s => s.CreateBuild(product.Id));
                    Log.Information($"BuildEngineCreateBuild: productId={product.Id}, projectName={product.Project.Name}");
                }
                else 
                {
                    throw new Exception($"Product \"{product.Id}\" does not have BuildEngine Product");
                }
            }
        }

        private static async Task<Product> GetProductForProcess(ProcessInstance processInstance, IJobRepository<Product, Guid> productRepository)
        {
            return await productRepository.Get()
                .Where(p => p.Id == processInstance.ProcessId)
                .Include(p => p.Project)
                    .ThenInclude(p => p.Owner)
                .FirstOrDefaultAsync();
        }

        private async Task SendOwnerNotificationAsync(ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter, CancellationToken token)
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                var productRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<Product, Guid>>();
                Product product = await GetProductForProcess(processInstance, productRepository);

                var owner = product?.Project.Owner;

                //TODO: Send Notification to user
                Log.Information($"SendNotification: auth0Id={owner.ExternalId}, name={owner.Name}");
            }
        }

        //
        // Actions from DWKit Samples (with name changes for Tables and Fields)
        //
        private async Task WriteProductTransitionAsync(ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter, CancellationToken token)
        {
            if (processInstance.IdentityIds == null)
                return;

            var currentstate = WorkflowInit.Runtime.GetLocalizedStateName(processInstance.ProcessId, processInstance.CurrentState);

            var nextState = WorkflowInit.Runtime.GetLocalizedStateName(processInstance.ProcessId, processInstance.ExecutedActivityState);

            var command = WorkflowInit.Runtime.GetLocalizedCommandName(processInstance.ProcessId, processInstance.CurrentCommand);

            using (var scope = ServiceProvider.CreateScope())
            {
                var userRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<User>>();
                var userNames = userRepository.Get()
                    .Where(u => processInstance.IdentityIds.Contains(u.WorkflowUserId.GetValueOrDefault().ToString()))
                    .Select(u => u.Name).ToList();
                var userNamesString = String.Join(',', userNames);
                var productTransitionsRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<ProductTransition>>();
                var history = new ProductTransition
                {
                    ProductId = processInstance.ProcessId,
                    AllowedUserNames = userNamesString,
                    InitialState = currentstate,
                    DestinationState = nextState,
                    Command = command
                };
                await productTransitionsRepository.CreateAsync(history);
            }
        }

        private async Task UpdateProductTransitionAsync(ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter, CancellationToken token)
        {
            if (string.IsNullOrEmpty(processInstance.CurrentCommand))
                return;

            var currentstate = WorkflowInit.Runtime.GetLocalizedStateName(processInstance.ProcessId, processInstance.CurrentState);

            var nextState = WorkflowInit.Runtime.GetLocalizedStateName(processInstance.ProcessId, processInstance.ExecutedActivityState);

            var command = WorkflowInit.Runtime.GetLocalizedCommandName(processInstance.ProcessId, processInstance.CurrentCommand);

            var isTimer = !string.IsNullOrEmpty(processInstance.ExecutedTimer);

            using (var scope = ServiceProvider.CreateScope())
            {
                var productTransitionsRepository = scope.ServiceProvider.GetRequiredService<IJobRepository<ProductTransition>>();
                var history = await productTransitionsRepository.Get()
                    .Where(h => h.ProductId == processInstance.ProcessId
                           && h.DateTransition == null
                           && h.InitialState == currentstate
                           && h.DestinationState == nextState).FirstOrDefaultAsync();
                if (history == null)
                {
                    history = new ProductTransition
                    {
                        ProductId = processInstance.ProcessId,
                        AllowedUserNames = String.Empty,
                        InitialState = currentstate,
                        DestinationState = nextState
                    };
                    history = await productTransitionsRepository.CreateAsync(history);
                }

                history.Command = !isTimer ? command : string.Format("Timer: {0}", processInstance.ExecutedTimer);
                history.DateTransition = DateTime.UtcNow;
                if (Guid.TryParse(processInstance.IdentityId, out Guid identityId))
                {
                    history.WorkflowUserId = identityId;
                }
                await productTransitionsRepository.UpdateAsync(history);
            }
        }


        #region Implementation of IWorkflowActionProvider

        public void ExecuteAction(string name, ProcessInstance processInstance, WorkflowRuntime runtime,
            string actionParameter)
        {
            if (_actions.ContainsKey(name))
                _actions[name].Invoke(processInstance, runtime, actionParameter);
            else
                throw new NotImplementedException($"Action with name {name} isn't implemented");
        }

        public async Task ExecuteActionAsync(string name, ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter, CancellationToken token)
        {
            //token.ThrowIfCancellationRequested(); // You can use the transferred token at your discretion
            if (_asyncActions.ContainsKey(name))
                await _asyncActions[name].Invoke(processInstance, runtime, actionParameter, token);
            else
                throw new NotImplementedException($"Async Action with name {name} isn't implemented");
        }

        public bool ExecuteCondition(string name, ProcessInstance processInstance, WorkflowRuntime runtime,
            string actionParameter)
        {
            if (_conditions.ContainsKey(name))
                return _conditions[name].Invoke(processInstance, runtime, actionParameter);

            throw new NotImplementedException($"Condition with name {name} isn't implemented");
        }

        public async Task<bool> ExecuteConditionAsync(string name, ProcessInstance processInstance, WorkflowRuntime runtime, string actionParameter, CancellationToken token)
        {
            //token.ThrowIfCancellationRequested(); // You can use the transferred token at your discretion
            if (_asyncConditions.ContainsKey(name))
                return await _asyncConditions[name].Invoke(processInstance, runtime, actionParameter, token);

            throw new NotImplementedException($"Async Condition with name {name} isn't implemented");
        }

        public bool IsActionAsync(string name)
        {
            return _asyncActions.ContainsKey(name);
        }

        public bool IsConditionAsync(string name)
        {
            return _asyncConditions.ContainsKey(name);
        }

        public List<string> GetActions()
        {
            var actions = _actions.Keys.Union(_asyncActions.Keys).ToList();
            return actions;
        }

        public List<string> GetConditions()
        {
            var conditions = _conditions.Keys.Union(_asyncConditions.Keys).ToList();
                return conditions;
        }

        #endregion
    }
}
