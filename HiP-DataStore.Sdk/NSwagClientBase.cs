using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.DataStore
{
    public abstract class NSwagClientBase
    {
        /// <summary>
        /// The value for the HTTP Authorization header,
        /// e.g. "Bearer [Your JWT token here]".
        /// </summary>
        public string Authorization { get; set; }

        /// <summary>
        /// Method to construct and configure an <see cref="HttpClient"/>.
        /// If null, <see cref="HttpClient"/> instances are created with default settings.
        /// </summary>
        public Func<HttpClient> CreateHttpClient { get; set; }

        protected Task<HttpClient> CreateHttpClientAsync(CancellationToken cancellationToken)
        {
            var http = CreateHttpClient?.Invoke() ?? new HttpClient();
            if (!string.IsNullOrEmpty(Authorization))
                http.DefaultRequestHeaders.Add("Authorization", Authorization);
            return Task.FromResult(http);
        }
    }
}
