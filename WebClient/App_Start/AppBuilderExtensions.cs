using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using Owin;
using WebClient.Providers;

namespace WebClient
{
    public static class AppBuilderExtensions
    {
        public static void UseOAuthBearerTokens(this IAppBuilder app, OAuthAuthorizationServerOptions options,
            string externalAuthenticationType)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            app.UseOAuthAuthorizationServer(options);

            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = options.AccessTokenFormat,
                AccessTokenProvider = options.AccessTokenProvider,
                AuthenticationMode = options.AuthenticationMode,
                AuthenticationType = options.AuthenticationType,
                Description = options.Description,
                Provider = new ApplicationOAuthBearerProvider(),
                SystemClock = options.SystemClock
            });

            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = options.AccessTokenFormat,
                AccessTokenProvider = options.AccessTokenProvider,
                AuthenticationMode = AuthenticationMode.Passive,
                AuthenticationType = externalAuthenticationType,
                Description = options.Description,
                Provider = new ExternalOAuthBearerProvider(),
                SystemClock = options.SystemClock
            });
        }
    }
}