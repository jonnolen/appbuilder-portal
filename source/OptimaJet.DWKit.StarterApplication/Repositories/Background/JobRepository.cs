﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JsonApiDotNetCore.Data;
using JsonApiDotNetCore.Internal.Query;
using JsonApiDotNetCore.Models;
using Microsoft.EntityFrameworkCore;


namespace OptimaJet.DWKit.StarterApplication.Repositories
{
    public class JobRepository<TEntity>
        : JobRepository<TEntity, int>,
        IJobRepository<TEntity>
        where TEntity : class, IIdentifiable<int>
    {
        public JobRepository(IDbContextResolver contextResolver)
            : base(contextResolver)
        {}
    }

    public class JobRepository<TEntity, TId> 
        : IJobRepository<TEntity, TId>
        where TEntity : class, IIdentifiable<TId>
    {
        protected readonly DbSet<TEntity> dbSet;
        protected readonly DbContext dbContext;

        public JobRepository(IDbContextResolver contextResolver)
        {
            this.dbContext = contextResolver.GetContext();
            this.dbSet = contextResolver.GetDbSet<TEntity>();
        }

        public virtual async Task<TEntity> CreateAsync(TEntity entity)
        {
            dbSet.Add(entity);

            var retval = await dbContext.SaveChangesAsync();
            return entity;
        }

        public virtual IQueryable<TEntity> Get()
        {
            return dbSet;
        }

        public virtual Task<List<TEntity>> GetListAsync()
        {
            return dbSet.ToListAsync();
        }
        public virtual async Task<TEntity> GetAsync(TId id)
        {
            return await Get().SingleOrDefaultAsync(e => e.Id.Equals(id));
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            if (entity == null) return null;

            dbSet.Update(entity);
            await dbContext.SaveChangesAsync();
            return entity;
        }
        public virtual async Task<bool> DeleteAsync(TId id)
        {
            var entity = await GetAsync(id);
            if (entity == null) return false;

            dbSet.Remove(entity);
            await dbContext.SaveChangesAsync();
            return true;
        }
    }
}
