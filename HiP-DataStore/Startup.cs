using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using PaderbornUniversity.SILab.Hip.EventSourcing;
using PaderbornUniversity.SILab.Hip.Webservice;
using Swashbuckle.AspNetCore.Swagger;

namespace PaderbornUniversity.SILab.Hip.DataStore
{
    public class Startup
    {
        private const string Version = "v1";
        private const string Name = "HiP Data Store API";

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Register the Swagger generator
            services.AddSwaggerGen(c =>
            {
                // Define a Swagger document
                c.SwaggerDoc("v1", new Info { Title = Name, Version = Version });
                c.OperationFilter<SwaggerOperationFilter>();
                c.OperationFilter<SwaggerFileUploadOperationFilter>();
                c.DescribeAllEnumsAsStrings();
            });

            services.Configure<EndpointConfig>(Configuration.GetSection("Endpoints"))
                    .Configure<UploadFilesConfig>(Configuration.GetSection("UploadingFiles"))
                    .Configure<ExhibitPagesConfig>(Configuration.GetSection("ExhibitPages"))
                    .Configure<AuthConfig>(Configuration.GetSection("Auth"))
                    .Configure<CorsConfig>(Configuration);

            string domain = Configuration.GetSection("Auth").GetValue<string>("Authority");
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
            services.AddSingleton<EventStoreClient>()
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
            IOptions<AuthConfig> authConfig, IOptions<CorsConfig> corsConfig, IOptions<EndpointConfig> endpointConfig)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"))
                         .AddDebug();

            // CacheDatabaseManager should start up immediately (not only when injected into a controller or
            // something), so we manually request an instance here
            app.ApplicationServices.GetService<CacheDatabaseManager>();

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

            var options = new JwtBearerOptions
            {
                Audience = authConfig.Value.Audience,
                Authority = authConfig.Value.Authority
            };
            app.UseJwtBearerAuthentication(options);

            app.UseMvc();

            // Swagger / Swashbuckle configuration:
            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Host = httpReq.Host.Value);
            });

            // Configure SwaggerUI endpoint
            app.UseSwaggerUI(c =>
            {
                var swaggerJsonUrl = string.IsNullOrEmpty(endpointConfig.Value.SwaggerEndpoint)
                    ? $"/swagger/{Version}/swagger.json"
                    : endpointConfig.Value.SwaggerEndpoint;

                c.SwaggerEndpoint(swaggerJsonUrl, $"{Name} {Version}");
            });
        }
    }
}
