using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using PaderbornUniversity.SILab.Hip.EventSourcing.FakeStore;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo.Test;
using PaderbornUniversity.SILab.Hip.Webservice;
using System.Collections.Generic;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests
{
    public class TestStartup
    {
        private static readonly Dictionary<string, string> _appsettings = new Dictionary<string, string>
        {
            { "EventStore:Host", "" },
            { "EventStore:Stream", "test" }
        };

        public TestStartup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(_appsettings)
                .SetBasePath(env.ContentRootPath)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            // Initialize ResourceTypes
            ResourceTypes.Initialize();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .Configure<EndpointConfig>(Configuration.GetSection("Endpoints"))
                .Configure<MongoDbConfig>(Configuration.GetSection("Endpoints"))
                .Configure<EventStoreConfig>(Configuration.GetSection("EventStore"))
                .Configure<UploadFilesConfig>(Configuration.GetSection("UploadingFiles"))
                .Configure<ExhibitPagesConfig>(Configuration.GetSection("ExhibitPages"))
                .Configure<AuthConfig>(Configuration.GetSection("Auth"))
                .Configure<CorsConfig>(Configuration);

            var serviceProvider = services.BuildServiceProvider(); // allows us to actually get the configured services           
            var authConfig = serviceProvider.GetService<IOptions<AuthConfig>>();

            // Configure authentication
            services
                .AddAuthentication(FakeAuthentication.AuthenticationScheme)
                .AddFakeAuthenticationScheme();

            // Configure authorization
            var domain = authConfig.Value.Authority;
            services.AddAuthorization(options =>
            {
                options.AddPolicy("read:datastore",
                    policy => policy.Requirements.Add(new HasScopeRequirement("read:datastore", domain)));
                options.AddPolicy("write:datastore",
                    policy => policy.Requirements.Add(new HasScopeRequirement("write:datastore", domain)));
                options.AddPolicy("write:cms",
                    policy => policy.Requirements.Add(new HasScopeRequirement("write:cms", domain)));
            });

            services.AddCors();
            services.AddMvc();

            services
                .AddSingleton<IEventStore, FakeEventStore>()
                .AddSingleton<IMongoDbContext, FakeMongoDbContext>()
                .AddSingleton<EventStoreService>()
                .AddSingleton<CacheDatabaseManager>()
                .AddSingleton<InMemoryCache>()
                .AddSingleton<IDomainIndex, MediaIndex>()
                .AddSingleton<IDomainIndex, EntityIndex>()
                .AddSingleton<IDomainIndex, ReferencesIndex>()
                .AddSingleton<IDomainIndex, TagIndex>()
                .AddSingleton<IDomainIndex, ExhibitPageIndex>()
                .AddSingleton<IDomainIndex, ScoreBoardIndex>()
                .AddSingleton<IDomainIndex, RatingIndex>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            IOptions<EndpointConfig> endpointConfig)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"))
                         .AddDebug();

            // CacheDatabaseManager should start up immediately (not only when injected into a controller or
            // something), so we manually request an instance here
            app.ApplicationServices.GetService<CacheDatabaseManager>();

            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
