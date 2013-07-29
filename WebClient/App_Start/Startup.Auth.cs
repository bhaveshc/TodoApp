using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security.OAuth;
using Owin;
using WebClient.Providers;

namespace WebClient
{
    public partial class Startup
    {
        public const string ExternalOAuthAuthenticationType = "ExternalToken";

        static Startup()
        {
            PublicClientId = "self";

            OAuthOptions = new OAuthAuthorizationServerOptions
            {
                TokenEndpointPath = "/Token",
                AuthorizeEndpointPath = "/api/Account/ExternalLogin",
                Provider = new ApplicationOAuthProvider(PublicClientId, IdentityConfig.Settings,
                    () => new IdentityStoreContext())
            };
        }

        public static OAuthAuthorizationServerOptions OAuthOptions { get; private set; }

        public static string PublicClientId { get; private set; }

        // For more information on configuring authentication, please visit http://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Enable the application to use bearer tokens to authenticate users
            app.UseOAuthBearerTokens(OAuthOptions, ExternalOAuthAuthenticationType);

            // Enable the application to use a cookie to store temporary information about a user logging in with a third party login provider
            app.UseExternalSignInCookie();

            // Uncomment the following lines to enable logging in with third party login providers
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //    consumerKey: "",
            //    consumerSecret: "");

            //app.UseFacebookAuthentication(
            //    appId: "",
            //    appSecret: "");

            //app.UseGoogleAuthentication();
        }
    }
}
