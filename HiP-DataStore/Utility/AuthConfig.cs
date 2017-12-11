namespace PaderbornUniversity.SILab.Hip.DataStore.Utility
{
    public class AuthConfig
    {
        /// <summary>
        /// Audience of all HiP APIs.
        /// Default value: "https://hip.cs.upb.de/API"
        /// </summary>
        public string Audience { get; set; } = "https://hip.cs.upb.de/API";

        /// <summary>
        /// Authority.
        /// Default value: "https://hip.eu.auth0.com/"
        /// </summary>
        public string Authority { get; set; } = "https://hip.eu.auth0.com/";
    }
}
