using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests
{
    public static class FakeAuthentication
    {
        public const string AuthenticationScheme = "FakeScheme";

        public static AuthenticationBuilder AddFakeAuthenticationScheme(this AuthenticationBuilder authenticationBuilder) =>
            authenticationBuilder.AddScheme<FakeAuthenticationOptions, FakeAuthenticationHandler>(
                AuthenticationScheme, options => { });
    }

    /// <summary>
    /// Fakes authentication/authorization by pretending there's an authenticated user.
    /// 
    /// The ID and the role of the fake user are taken from the HTTP header "Authorization",
    /// so this header should not (as usual) contain an auth token, but rather the user ID and
    /// role separated by a dash, e.g. "SampleAdmin-Administrator".
    /// 
    /// Requirements (e.g. "read:datastore") are not checked, we just pretend that they are fulfilled.
    /// 
    /// Written with guidance from https://github.com/aspnet/Security/issues/1360.
    /// </summary>
    public class FakeAuthenticationHandler : AuthenticationHandler<FakeAuthenticationOptions>
    {
        public FakeAuthenticationHandler(
            IOptionsMonitor<FakeAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var userAndRole = ((string)Context.Request.Headers["Authorization"])?.Split('-');

            if (userAndRole == null)
                return Task.FromResult(AuthenticateResult.NoResult());

            var userId = userAndRole[0];
            var role = userAndRole.Length > 1 ? userAndRole[1] : "";

            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim("https://hip.cs.upb.de/sub", userId),
                    new Claim("https://hip.cs.upb.de/roles", role)
                },
                FakeAuthentication.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, identity.AuthenticationType);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    public class FakeAuthenticationOptions : AuthenticationSchemeOptions
    {
    }
}