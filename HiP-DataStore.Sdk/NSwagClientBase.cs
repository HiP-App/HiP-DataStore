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

        protected Task<HttpClient> CreateHttpClientAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpClient
            {
                DefaultRequestHeaders = { { "Authorization", Authorization } }
            });
        }
    }
}
