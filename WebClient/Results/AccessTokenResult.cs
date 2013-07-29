using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WebClient.Results
{
    public class AccessTokenResult : IHttpActionResult
    {
        public AccessTokenResult(string userId, OAuthAuthorizationServerOptions oAuthOptions,
            IdentityStoreManager identityStore, ApiController controller)
        {
            UserId = userId;
            OAuthOptions = oAuthOptions;
            IdentityStore = identityStore;
            Request = controller.Request;
        }

        public string UserId { get; private set; }
        public OAuthAuthorizationServerOptions OAuthOptions { get; set; }
        public IdentityStoreManager IdentityStore { get; set; }
        public HttpRequestMessage Request { get; private set; }

        public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            ClaimsIdentity identity = await GetIdentityAsync(UserId);
            TokenContent content = CreateContent(identity);
            JsonResult<TokenContent> innerResult = new JsonResult<TokenContent>(content, new JsonSerializerSettings(),
                Encoding.UTF8, Request);
            return await innerResult.ExecuteAsync(cancellationToken);
        }

        private string CreateAccessToken(ClaimsIdentity identity)
        {
            return OAuthOptions.AccessTokenFormat.Protect(new AuthenticationTicket(identity,
                   new AuthenticationProperties()));
        }

        private TokenContent CreateContent(ClaimsIdentity identity)
        {
            return new TokenContent
            {
                AccessToken = CreateAccessToken(identity),
                TokenType = "bearer",
                ExpiresIn = OAuthOptions.AccessTokenExpireTimeSpan.Seconds,
                UserName = identity.FindFirstValue(IdentityStore.Settings.GetAuthenticationOptions().UserNameClaimType)
            };
        }

        private async Task<ClaimsIdentity> GetIdentityAsync(string userId)
        {
            IList<Claim> claims = await IdentityStore.GetUserIdentityClaimsAsync(userId, new Claim[0],
                CancellationToken.None);

            if (claims == null)
            {
                return null;
            }

            IdentityAuthenticationOptions authenticationOptions = IdentityStore.Settings.GetAuthenticationOptions();

            return new ClaimsIdentity(claims, OAuthOptions.AuthenticationType, authenticationOptions.UserNameClaimType,
                authenticationOptions.RoleClaimType);
        }

        private class TokenContent
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; }

            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int? ExpiresIn { get; set; }

            [JsonProperty("userName")]
            public string UserName { get; set; }
        }
    }
}
