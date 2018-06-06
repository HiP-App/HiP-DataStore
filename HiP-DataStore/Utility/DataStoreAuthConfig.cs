
using PaderbornUniversity.SILab.Hip.Webservice;

namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public class DataStoreAuthConfig : AuthConfig
    {
        /// <summary>
        /// Auth0 domain.
        /// Default value: "hip.eu.auth0.com"
        /// </summary>
        public string Domain { get; set; } = "hip.eu.auth0.com";

        /// <summary>
        /// ID of the non-interactive UserStore client (used to access Auth0 Management API).
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Secret of the non-interactive UserStore client (used to access Auth0 Management API).
        /// </summary>
        public string ClientSecret { get; set; }
    }
}
