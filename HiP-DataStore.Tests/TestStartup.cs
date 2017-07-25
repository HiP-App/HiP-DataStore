﻿using System;
using EventStore.ClientAPI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaderbornUniversity.SILab.Hip.DataStore.Core;
using PaderbornUniversity.SILab.Hip.DataStore.Core.ReadModel;
using PaderbornUniversity.SILab.Hip.DataStore.Core.WriteModel;
using PaderbornUniversity.SILab.Hip.DataStore.Utility;
using Microsoft.Extensions.Options;

namespace PaderbornUniversity.SILab.Hip.DataStore.Tests
{
    public class TestStartup
    {
        public IConfigurationRoot Configuration { get; }

        public TestStartup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .Configure<EndpointConfig>(Configuration.GetSection("Endpoints"))
                .Configure<UploadFilesConfig>(Configuration.GetSection("UploadingFiles"))
                .Configure<ExhibitPagesConfig>(Configuration.GetSection("ExhibitPages"))
                .Configure<CorsConfig>(Configuration);
            
            // Add framework services
            services.AddMvc();

            services
                .AddSingleton<EventStoreClient>()
                .AddSingleton<CacheDatabaseManager>()
                .AddSingleton<IDomainIndex, MediaIndex>()
                .AddSingleton<IDomainIndex, EntityIndex>()
                .AddSingleton<IDomainIndex, ReferencesIndex>()
                .AddSingleton<IDomainIndex, TagIndex>()
                .AddSingleton<IDomainIndex, ExhibitPageIndex>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IOptions<EndpointConfig> config)
        {
            app.UseMvc();
            MvcTestContext.Services = app.ApplicationServices;

            // Clear the event stream so that we start with an empty database for each test
            // (ideally we should not depend on running instances of Event Store and MongoDB. Instead we should
            // mock these connections by creating "fake" connections that just store events/data in memory)
            var settings = ConnectionSettings.Create().EnableVerboseLogging().Build();
            var connection = EventStoreConnection.Create(settings, new Uri(config.Value.EventStoreHost));
            connection.ConnectAsync().Wait();
            connection.DeleteStreamAsync(config.Value.EventStoreStream, ExpectedVersion.Any).Wait();
        }
    }
}