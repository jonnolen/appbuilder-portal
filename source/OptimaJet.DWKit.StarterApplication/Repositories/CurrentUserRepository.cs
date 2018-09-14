using System.Linq;
using System.Threading.Tasks;
using JsonApiDotNetCore.Data;
using JsonApiDotNetCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Optimajet.DWKit.StarterApplication.Data;
using Optimajet.DWKit.StarterApplication.Models;
using OptimaJet.DWKit.StarterApplication.Services;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;

namespace OptimaJet.DWKit.StarterApplication.Repositories
{
    public class CurrentUserRepository : DefaultEntityRepository<User> 
    {
        // NOTE: this repository MUST not rely on any other repositories or services
        public CurrentUserRepository(
            ILoggerFactory loggerFactory,
            IJsonApiContext jsonApiContext,
            IDbContextResolver contextResolver,
            ICurrentUserContext currentUserContext
        ) : base(loggerFactory, jsonApiContext, contextResolver)
        {
            this.DBContext = (AppDbContext)contextResolver.GetContext();
            this.CurrentUserContext = currentUserContext;
        }

        public AppDbContext DBContext { get; }
        public ICurrentUserContext CurrentUserContext { get; }

        // memoize once per local thread,
        // since the current user can't change in a single request
        // this should be ok.
        public async Task<User> GetCurrentUser()
        {
            var auth0Id = this.CurrentUserContext.Auth0Id;

            var userFromResult = this.DBContext
                .Users.Local
                .FirstOrDefault(u => u.ExternalId.Equals(auth0Id));

            if (userFromResult != null) {
                return await Task.FromResult(userFromResult);
            }

            var currentUser = await base.Get()
                .Where(user => user.ExternalId.Equals(auth0Id))
                .FirstOrDefaultAsync();

            return currentUser;
        }
    }
}