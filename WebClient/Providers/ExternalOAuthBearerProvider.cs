using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin.Security.OAuth;

namespace WebClient.Providers
{
    public class ExternalOAuthBearerProvider : OAuthBearerAuthenticationProvider
    {
        public override Task ValidateIdentity(OAuthValidateIdentityContext context)
        {
            if (context.Ticket.Identity.Claims.Count() == 0)
            {
                context.Rejected();
            }
            else if (context.Ticket.Identity.Claims.All(c => c.Issuer == ClaimsIdentity.DefaultIssuer))
            {
                context.Rejected();
            }

            return Task.FromResult<object>(null);
        }
    }
}