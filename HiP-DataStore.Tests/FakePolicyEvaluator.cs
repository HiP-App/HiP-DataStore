using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests
{
    public class TestUserInjector
    {
        /// <summary>
        /// The name of the test user.
        /// </summary>
        public static string Name { get; set; }

        /// <summary>
        /// The role of the test user.
        /// </summary>
        public static string Role { get; set; }

        public static async Task InvokeAsync(HttpContext context, Func<Task> next)
        {
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim("https://hip.cs.upb.de/sub", Name),
                    new Claim("https://hip.cs.upb.de/roles", Role)
                },
                "FakeScheme");

            var principal = new ClaimsPrincipal(identity);
            context.User = principal;

            await next();
        }
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
    /// Adapted from https://github.com/aspnet/Security/issues/1360.
    /// </summary>
    public class FakePolicyEvaluator : IPolicyEvaluator
    {
        public Task<AuthenticateResult> AuthenticateAsync(AuthorizationPolicy policy, HttpContext context)
        {
            var userAndRole = ((string)context.Request.Headers["Authorization"]).Split('-');

            var userId = userAndRole[0];
            var role = userAndRole.Length > 1 ? userAndRole[1] : "";

            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim("https://hip.cs.upb.de/sub", userId),
                    new Claim("https://hip.cs.upb.de/roles", role)
                },
                "FakeScheme");

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, identity.AuthenticationType);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        public Task<PolicyAuthorizationResult> AuthorizeAsync(AuthorizationPolicy policy, AuthenticateResult authenticationResult, HttpContext context, object resource) =>
            Task.FromResult(PolicyAuthorizationResult.Success());
    }
}