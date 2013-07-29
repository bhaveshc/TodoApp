using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin.Security.OAuth;

namespace WebClient.Providers
{
    public class ApplicationOAuthBearerProvider : OAuthBearerAuthenticationProvider
    {
        public override Task ValidateIdentity(OAuthValidateIdentityContext context)
        {
            if (context.Ticket.Identity.Claims.Any(c => c.Issuer != ClaimsIdentity.DefaultIssuer))
            {
                context.Rejected();
            }

            return Task.FromResult<object>(null);
        }
    }
}