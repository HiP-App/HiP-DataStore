using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PaderbornUniversity.SILab.Hip.DataStore
{
    /// <summary>
    /// A service that can be used with ASP.NET Core dependency injection.
    /// Usage: In ConfigureServices():
    /// <code>
    /// services.Configure&lt;DataStoreConfig&gt;(Configuration.GetSection("Endpoints"));
    /// services.AddSingleton&lt;DataStoreService&gt;();
    /// </code>
    /// </summary>
    public class DataStoreService
    {
        private readonly DataStoreConfig _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DataStoreService(IOptions<DataStoreConfig> config, ILogger<DataStoreService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _config = config.Value;
            _httpContextAccessor = httpContextAccessor;

            if (string.IsNullOrWhiteSpace(config.Value.DataStoreHost))
                logger.LogWarning($"{nameof(DataStoreConfig.DataStoreHost)} is not configured correctly!");
        }

        public ExhibitPagesClient ExhibitPages => new ExhibitPagesClient(_config.DataStoreHost)
        {
            Authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
        };

        public ExhibitsClient Exhibits => new ExhibitsClient(_config.DataStoreHost)
        {
            Authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
        };

        public HistoryClient History => new HistoryClient(_config.DataStoreHost)
        {
            Authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
        };

        public MediaClient Media => new MediaClient(_config.DataStoreHost)
        {
            Authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
        };

        public RoutesClient Routes => new RoutesClient(_config.DataStoreHost)
        {
            Authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
        };

        public ScoreBoardClient ScoreBoard => new ScoreBoardClient(_config.DataStoreHost)
        {
            Authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
        };

        public StatusesClient Statuses => new StatusesClient(_config.DataStoreHost)
        {
            Authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
        };

        public TagsClient Tags => new TagsClient(_config.DataStoreHost)
        {
            Authorization = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"]
        };
    }
}
