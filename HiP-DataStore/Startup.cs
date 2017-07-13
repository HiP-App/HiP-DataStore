﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
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
                    .Configure<CorsConfig>(Configuration);

            services.AddCors();
            services.AddMvc();
            services.AddSingleton<EventStoreClient>()
                    .AddSingleton<CacheDatabaseManager>()
                    .AddSingleton<IDomainIndex, MediaIndex>()
                    .AddSingleton<IDomainIndex, EntityIndex>()
                    .AddSingleton<IDomainIndex, ReferencesIndex>()
                    .AddSingleton<IDomainIndex, TagIndex>()
                    .AddSingleton<IDomainIndex, ExhibitPageIndex>()
                    .AddSingleton<IDomainIndex, ScoreBoardIndex>()
                    .AddSingleton<IDomainIndex, RatingIndex>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IOptions<CorsConfig> corsConfig)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"))
                         .AddDebug();

            // CacheDatabaseManager should start up immediately (not only when injected into a controller or
            // something), so we manually request an instance here
            app.ApplicationServices.GetService<CacheDatabaseManager>();

            // Use CORS (important: must be before app.UseMvc())
            app.UseCors(builder => {
                var corsEnvConf = corsConfig.Value.Cors[env.EnvironmentName];
                builder
                    .WithOrigins(corsEnvConf.Origins)
                    .WithMethods(corsEnvConf.Methods)
                    .WithHeaders(corsEnvConf.Headers)
                    .WithExposedHeaders(corsEnvConf.ExposedHeaders);
            }); 

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
                // TODO: Only a hack, if HiP-Swagger is running, SwaggerUI can be disabled for Production
                c.SwaggerEndpoint((env.IsDevelopment() ? "/swagger" : "..") +
                                  "/" + Version + "/swagger.json", Name + Version);
            });
        }
    }
}
