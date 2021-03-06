﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSwag.AspNetCore;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Model;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.EventSourcing.EventStoreLlp;
using PaderbornUniversity.SILab.Hip.EventSourcing.Mongo;
using PaderbornUniversity.SILab.Hip.UserStore;
using PaderbornUniversity.SILab.Hip.Webservice;
using PaderbornUniversity.SILab.Hip.Webservice.Logging;

namespace PaderbornUniversity.SILab.Hip.DataStore
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
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
                .Configure<UserStoreConfig>(Configuration.GetSection("Endpoints"))
                .Configure<EventStoreConfig>(Configuration.GetSection("EventStore"))
                .Configure<UploadFilesConfig>(Configuration.GetSection("UploadingFiles"))
                .Configure<ExhibitPagesConfig>(Configuration.GetSection("ExhibitPages"))
                .Configure<DataStoreAuthConfig>(Configuration.GetSection("Auth"))
                .Configure<LoggingConfig>(Configuration.GetSection("HiPLoggerConfig"))
                .Configure<CorsConfig>(Configuration);

            var serviceProvider = services.BuildServiceProvider(); // allows us to actually get the configured services           
            var authConfig = serviceProvider.GetService<IOptions<DataStoreAuthConfig>>();

            // Configure authentication
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Audience = authConfig.Value.Audience;
                    options.Authority = authConfig.Value.Authority;
                });

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
                .AddSingleton<IEventStore, EventSourcing.EventStoreLlp.EventStore>()
                .AddSingleton<IMongoDbContext, MongoDbContext>()
                .AddSingleton<EventStoreService>()
                .AddSingleton<UserStoreService>()
                .AddSingleton<CacheDatabaseManager>()
                .AddSingleton<InMemoryCache>()
                .AddSingleton<IDomainIndex, MediaIndex>()
                .AddSingleton<IDomainIndex, EntityIndex>()
                .AddSingleton<IDomainIndex, ReferencesIndex>()
                .AddSingleton<IDomainIndex, TagIndex>()
                .AddSingleton<IDomainIndex, ExhibitPageIndex>()
                .AddSingleton<IDomainIndex, ScoreBoardIndex>()
                .AddSingleton<IDomainIndex, RatingIndex>()
                .AddSingleton<IDomainIndex, ReviewIndex>()
                .AddSingleton<IDomainIndex,HighScoreIndex>()
                .AddSingleton<IDomainIndex, ReviewCommentIndex>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory,
            IOptions<CorsConfig> corsConfig, IOptions<LoggingConfig> loggingConfig)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"))
                .AddDebug()
                .AddHipLogger(loggingConfig.Value);

            var variables = Configuration.GetSection("EventStore").AsEnumerable();
            var logger = loggerFactory.CreateLogger("Test");
            foreach (var v in variables)
            {
                logger.LogInformation(v.ToString());
            }

            // CacheDatabaseManager should start up immediately (not only when injected into a controller or
            // something), so we manually request an instance here
            app.ApplicationServices.GetService<CacheDatabaseManager>();

            // Ensures that "Request.Scheme" is correctly set to "https" in our nginx-environment
            app.UseRequestSchemeFixer();

            // Use CORS (important: must be before app.UseMvc())
            app.UseCors(builder =>
            {
                var corsEnvConf = corsConfig.Value.Cors[env.EnvironmentName];
                builder
                    .WithOrigins(corsEnvConf.Origins)
                    .WithMethods(corsEnvConf.Methods)
                    .WithHeaders(corsEnvConf.Headers)
                    .WithExposedHeaders(corsEnvConf.ExposedHeaders);
            });

            app.UseAuthentication();
            app.UseMvc();
            app.UseSwaggerUiHip();

            loggerFactory.CreateLogger("ApplicationStartup").LogInformation("DataStore started successfully");
        }
    }
}
