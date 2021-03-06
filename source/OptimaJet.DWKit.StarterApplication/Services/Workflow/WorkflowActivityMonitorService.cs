﻿using System;
using System.Linq;
using System.Threading.Tasks;
using OptimaJet.Workflow.Core.Persistence;
using OptimaJet.Workflow.Core.Runtime;
using OptimaJet.DWKit.StarterApplication.Utility;
using Serilog;
using OptimaJet.DWKit.StarterApplication.Repositories;
using OptimaJet.DWKit.StarterApplication.Models;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using System.Collections.Generic;
using OptimaJet.DWKit.Core.Metadata.DbObjects;
using OptimaJet.DWKit.Core;

namespace OptimaJet.DWKit.StarterApplication.Services.Workflow
{
    public class WorkflowActivityMonitorService
    {
        public WorkflowRuntime Runtime { get; set; }

        public void RegisterEventHandlers(WorkflowRuntime runtime)
        {
            Runtime = runtime;
            Runtime.ProcessActivityChanged += (sender, args) => { ActivityChanged(args, Runtime); };
            Runtime.ProcessStatusChanged += (sender, args) => { ProcessChanged(args, Runtime); };
            Runtime.OnWorkflowError += (sender, args) => { ProcessException(args, Runtime); };
        }

        private void ProcessException(WorkflowErrorEventArgs args, WorkflowRuntime runtime)
        {
            if (!String.IsNullOrEmpty(args.ProcessInstance.ExecutedTimer))
            {
                Log.Error($"Exception::: Timer: {args.Exception.Message}");
                args.ProcessStatus = ProcessStatus.Idled;
            }
            else
            {
                Log.Error($"Exception::: Command: {args.Exception.Message}");
                runtime.SetActivityWithExecution(
                    identityId: null,
                    impersonatedIdentityId: null,
                    parameters: new Dictionary<string, object>(),
                    activityToSet: args.ProcessInstance.ProcessScheme.InitialActivity,
                    processInstance: args.ProcessInstance,
                    doNotSetRunningStatus: true
                );
            }
        }

        private void ActivityChanged(ProcessActivityChangedEventArgs args, WorkflowRuntime runtime)
        {
            if (!args.TransitionalProcessWasCompleted)
                return;

            Log.Information($":::::::::: ActivityChanged: pid={args.ProcessId.ToString()}, scheme={args.SchemeCode}, activity={args.CurrentActivityName}, state={args.CurrentState}, last={args.PreviousState}");
            var serviceArgs = new WorkflowProductService.ProductProcessChangedArgs
            {
                ProcessId = args.ProcessId,
                CurrentActivityName = args.CurrentActivityName,
                PreviousActivityName = args.PreviousActivityName,
                CurrentState = args.CurrentState,
                PreviousState = args.PreviousState,
                ExecutingCommand = args.ProcessInstance.CurrentCommand
            };
            // There is a bit of a timing window before the ProcessId is assigned to the Product.  So delay this a little bit (15 seconds in the default minimum time.
            BackgroundJob.Enqueue<WorkflowProductService>(service => service.ProductProcessChanged(serviceArgs));

            //TODO change Document transition history and WorkflowInbox
        }

        private void ProcessChanged(ProcessStatusChangedEventArgs args, WorkflowRuntime runtime)
        {
            Log.Information($":::::::::: ProcessChanged: pid={args.ProcessId.ToString()}, scheme={args.SchemeCode}, new={args.NewStatus.StatusString()}, old={args.OldStatus.StatusString()}");
            if (String.IsNullOrWhiteSpace(args.ProcessInstance.CurrentActivityName)) return;
        }

        public void CheckActivityStatus()
        {
        }

        //
        // Note: It took a while to figure out how to iterate through all the current
        //       processes and try to restart them.  I thought I would need this to
        //       handle Timers getting cleared on startup.  Looks like they are not
        //       cleared.  Keeping this here just in case we need it in the future. -- ChrisH

        //public void RestartIdleActivites()
        //{
        //    RestartIdleActivitesAsync().Wait();
        //}
        //public async Task RestartIdleActivitesAsync()
        //{
        //    var procs = await WorkflowProcessInstance.SelectAsync(Filter.And.Equal(2 /*Idle*/, "InstanceStatus"));
        //    foreach (var proc in procs)
        //    {
        //        var procInstance = await Runtime.GetProcessInstanceAndFillProcessParametersAsync(proc.Id);
        //        Runtime.SetActivityWithExecution(
        //            identityId: null,
        //            impersonatedIdentityId: null,
        //            parameters: new Dictionary<string, object>(),
        //            activityToSet: procInstance.CurrentActivity,
        //            processInstance: procInstance,
        //            doNotSetRunningStatus: false
        //        );
        //    }
        //}
    }
}
