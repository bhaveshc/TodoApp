using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace WebClient
{
    public static class IdentitySettingsExtensions
    {
        public static IdentityAuthenticationOptions GetAuthenticationOptions(this IdentitySettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            return settings.Get<IdentityAuthenticationOptions>();
        }

        public static IdentityStoreManagerOptions GetStoreManagerOptions(this IdentitySettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            return settings.Get<IdentityStoreManagerOptions>();
        }
    }
}
