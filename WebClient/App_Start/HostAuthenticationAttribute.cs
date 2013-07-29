using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;

namespace WebClient
{
    public sealed class HostAuthenticationAttribute : Attribute, IAuthenticationFilter
    {
        private readonly HostAuthenticationFilter _innerFilter;

        public HostAuthenticationAttribute(string authenticationType)
        {
            _innerFilter = new HostAuthenticationFilter(authenticationType);
        }

        public string AuthenticationType
        {
            get { return _innerFilter.AuthenticationType; }
        }

        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            return _innerFilter.AuthenticateAsync(context, cancellationToken);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            return _innerFilter.ChallengeAsync(context, cancellationToken);
        }

        public bool AllowMultiple
        {
            get { return _innerFilter.AllowMultiple; }
        }
    }
}