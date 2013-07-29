using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using WebClient.Models;
using WebClient.Results;

namespace WebClient.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

        public AccountController()
            : this(new IdentityStoreManager(IdentityConfig.Settings,
                new IdentityStoreContext()), Startup.OAuthOptions)
        {
        }

        public AccountController(IdentityStoreManager identityStore, OAuthAuthorizationServerOptions oAuthOptions)
        {
            IdentityStore = identityStore;
            OAuthOptions = oAuthOptions;
        }

        public IdentityStoreManager IdentityStore { get; private set; }
        public OAuthAuthorizationServerOptions OAuthOptions { get; private set; }

        // GET api/Account/UserInfo
        [HttpGet("UserInfo")]
        public UserInfoViewModel UserInfo()
        {
            return new UserInfoViewModel
            {
                UserName = User.Identity.GetUserName()
            };
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [HttpGet("ManageInfo")]
        public async Task<ManageInfoViewModel> ManageInfo(string returnUrl, bool generateState = false)
        {
            IEnumerable<IUserLogin> linkedAccounts = await IdentityStore.GetLoginsAsync(User.Identity.GetUserId());
            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IUserLogin linkedAccount in linkedAccounts)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = IdentityStore.Settings.GetStoreManagerOptions().LocalLoginProvider,
                UserName = User.Identity.GetUserName(),
                Logins = logins,
                ExternalLoginProviders = ExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [HttpPost("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await IdentityStore.ChangePasswordAsync(User.Identity.GetUserName(),
                model.OldPassword, model.NewPassword);
            IHttpActionResult errorResult = GetErrorResult(result);

            if (errorResult != null)
            {
                return errorResult;
            }

            return Ok();
        }

        // POST api/Account/SetPassword
        [HttpPost("SetPassword")]
        public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Create the local login info and link the local account to the user
            IdentityResult result = await IdentityStore.CreateLocalLoginAsync(User.Identity.GetUserId(),
                User.Identity.GetUserName(), model.NewPassword);
            IHttpActionResult errorResult = GetErrorResult(result);

            if (errorResult != null)
            {
                return errorResult;
            }

            return Ok();
        }

        // POST api/Account/AddExternalLogin
        [HttpPost("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            AuthenticationTicket ticket = OAuthOptions.AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null)
            {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return InternalServerError();
            }

            // The current user is logged in, just add the new account
            IdentityResult result = await IdentityStore.AddLoginAsync(User.Identity.GetUserId(),
                externalData.LoginProvider, externalData.ProviderKey);
            IHttpActionResult errorResult = GetErrorResult(result);

            if (errorResult != null)
            {
                return errorResult;
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [HttpPost("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await IdentityStore.RemoveLoginAsync(User.Identity.GetUserId(),
                model.LoginProvider, model.ProviderKey);
            IHttpActionResult errorResult = GetErrorResult(result);

            if (errorResult != null)
            {
                return errorResult;
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication("External")]
        [AllowAnonymous]
        [HttpGet("ExternalLogin", RouteName = "ExternalLogin")]
        public IHttpActionResult ExternalLogin(string provider)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            Authentication.SignOutExternalIdentity();

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            IdentityAuthenticationOptions options = IdentityStore.Settings.GetAuthenticationOptions();
            ClaimsIdentity identity = externalLogin.ToIdentity(OAuthOptions.AuthenticationType,
                options.UserNameClaimType, options.RoleClaimType);
            Authentication.SignIn(identity);
            return Ok();
        }

        // GET api/Account/ExternalLoginComplete
        [OverrideAuthentication]
        [HostAuthentication(Startup.ExternalOAuthAuthenticationType)]
        [HttpGet("ExternalLoginComplete")]
        public async Task<IHttpActionResult> ExternalLoginComplete()
        {
            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            string userId = await IdentityStore.GetUserIdForLoginAsync(externalLogin.LoginProvider,
                externalLogin.ProviderKey);

            if (String.IsNullOrEmpty(userId))
            {
                return Content(HttpStatusCode.OK, new RegisterExternalLoginViewModel
                {
                    LoginProvider = externalLogin.LoginProvider,
                    UserName = externalLogin.UserName
                });
            }

            return AccessToken(userId);
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [HttpGet("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> ExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                state = GenerateAntiForgeryState();
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = returnUrl,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Create a profile, password, and link the local login before signing in the user
            User user = new User(model.UserName);
            IdentityResult result = await IdentityStore.CreateLocalUserAsync(user, model.Password);
            IHttpActionResult errorResult = GetErrorResult(result);

            if (errorResult != null)
            {
                return errorResult;
            }

            return AccessToken(user.Id);
        }

        // POST api/Account/RegisterExternal
        [OverrideAuthentication]
        [HostAuthentication(Startup.ExternalOAuthAuthenticationType)]
        [HttpPost("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            // Create a profile, and link the local account before signing in the user
            User user = new User(model.UserName);
            IdentityResult result = await IdentityStore.CreateExternalUserAsync(user, externalLogin.LoginProvider,
                externalLogin.ProviderKey);
            IHttpActionResult errorResult = GetErrorResult(result);

            if (errorResult != null)
            {
                return errorResult;
            }

            return AccessToken(user.Id);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IdentityStore.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IHttpActionResult AccessToken(string userId)
        {
            return new AccessTokenResult(userId, OAuthOptions, IdentityStore, this);
        }

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private string GenerateAntiForgeryState()
        {
            const int strengthInBits = 256;
            const int strengthInBytes = strengthInBits / 8;
            byte[] data = new byte[strengthInBytes];
            _random.GetBytes(data);
            return Convert.ToBase64String(data);
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Success)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public ClaimsIdentity ToIdentity(string authenticationType, string nameClaimType, string roleClaimType)
            {
                ClaimsIdentity identity = new ClaimsIdentity(authenticationType,
                    nameClaimType,
                    roleClaimType);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return identity;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        #endregion
    }
}
