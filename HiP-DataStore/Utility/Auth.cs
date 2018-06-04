using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public static class Auth
    {
        // Adds function to get User Id from Context.User.Identity
        public static string GetUserIdentity(this IIdentity identity)
        {
            return (identity as ClaimsIdentity)?.Claims
                .FirstOrDefault(c => c.Type == "https://hip.cs.upb.de/sub")?
                .Value;
        }

        public static IReadOnlyList<Claim> GetUserRoles(this IIdentity identity)
        {
            return (identity as ClaimsIdentity)?.FindAll(c => c.Type == "https://hip.cs.upb.de/roles").ToList() ?? new List<Claim>();
        }

        public static async Task<string> GetAccessTokenAsync(string domain,string audience,string clientID,string clientSecret)
        {
            var client = new AuthenticationApiClient(domain);

            var response = await client.GetTokenAsync(new ClientCredentialsTokenRequest
            {
                Audience = audience,
                ClientId = clientID,
                ClientSecret = clientSecret
            });

            return response.AccessToken;
        }
    }
}
