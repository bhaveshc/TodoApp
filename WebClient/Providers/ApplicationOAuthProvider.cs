using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;

namespace WebClient.Providers
{
    public class ApplicationOAuthProvider : OAuthAuthorizationServerProvider
    {
        private readonly string _publicClientId;
        private readonly IdentitySettings _settings;
        private readonly Func<IdentityStoreContext> _contextFactory;

        public ApplicationOAuthProvider(string publicClientId, IdentitySettings settings,
            Func<IdentityStoreContext> contextFactory)
        {
            if (publicClientId == null)
            {
                throw new ArgumentNullException("publicClientId");
            }

            if (settings == null)
            {
                throw new ArgumentNullException("identityStore");
            }

            if (contextFactory == null)
            {
                throw new ArgumentNullException("contextFactory");
            }

            _publicClientId = publicClientId;
            _settings = settings;
            _contextFactory = contextFactory;
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            using (IdentityStoreManager identityStore = new IdentityStoreManager(_settings, _contextFactory.Invoke()))
            {
                if (!await identityStore.ValidateLocalLoginAsync(context.UserName, context.Password))
                {
                    context.SetError("invalid_grant", "The user name or password is incorrect.");
                    return;
                }

                string userId = await identityStore.GetUserIdForLocalLoginAsync(context.UserName);
                IList<Claim> claims = await identityStore.GetUserIdentityClaimsAsync(userId, new Claim[0],
                    CancellationToken.None);
                ClaimsIdentity identity = new ClaimsIdentity(claims, context.Options.AuthenticationType,
                    identityStore.Settings.GetAuthenticationOptions().UserNameClaimType,
                    identityStore.Settings.GetAuthenticationOptions().RoleClaimType);
                IUser user = await identityStore.Context.Users.FindAsync(userId);
                IDictionary<string, string> extra = new Dictionary<string, string>
                {
                    { "userName", user.UserName }
                };
                AuthenticationTicket ticket = new AuthenticationTicket(identity, extra);
                context.Validated(ticket);
            }
        }

        public override Task LookupClient(OAuthLookupClientContext context)
        {
            if (context.ClientId == null || context.ClientId == _publicClientId)
            {
                context.ClientFound(null, null);
            }

            return Task.FromResult<object>(null);
        }

        public override Task TokenEndpoint(OAuthTokenEndpointContext context)
        {
            foreach (KeyValuePair<string, string> property in context.Properties.Dictionary)
            {
                context.AdditionalResponseParameters.Add(property.Key, property.Value);
            }

            return Task.FromResult<object>(null);
        }
    }
}