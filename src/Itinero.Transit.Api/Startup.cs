﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSwag;
using Reminiscence.Collections;
using Serilog;
using Serilog.Formatting.Json;
using Log = Serilog.Log;

namespace Itinero.Transit.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureLogging();

            State.FreeMessage = "Booting, loading config...";
            State.BootTime = DateTime.Now;

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyOrigin",
                    builder => builder.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET"));
            });

            services.AddSwaggerDocument();

            Task t = Task.Factory.StartNew(() =>
            {
                State.FreeMessage = "Loading transitdbs";
                State.TransitDb = Configuration.CreateTransitDbs();

                State.NameIndex = new NameIndexBuilder(new List<string> {"name:nl", "name", "name:fr"})
                    .Build(State.TransitDb.GetStopsReader());

/*
                Log.Information("Performing initial runs");
                foreach (var (name, (_, synchronizer)) in State.TransitDb.TransitDbs)
                {
                    State.FreeMessage = $"Initial data run, currently {name}";
                    synchronizer.InitialRun();
                }*/

                State.FreeMessage = "Starting clocks";
                Log.Information("Starting the synchronizers");
                foreach (var (_, (_, synchronizer)) in State.TransitDb.TransitDbs)
                {
                    synchronizer.Start();
                }

                State.FreeMessage = "Fully operational";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedHost |
                                   ForwardedHeaders.XForwardedProto
            };
            app.UseForwardedHeaders(options);
            app.Use((context, next) =>
            {
                if (context.Request.Headers.TryGetValue("X-Forwarded-PathBase", out var pathBases))
                {
                    context.Request.PathBase = pathBases.First();
                    if (context.Request.PathBase.Value.EndsWith("/"))
                    {
                        context.Request.PathBase =
                            context.Request.PathBase.Value.Substring(0, context.Request.PathBase.Value.Length - 1);
                    }

                    if (context.Request.Path.Value.StartsWith(context.Request.PathBase.Value))
                    {
                        var before = context.Request.Path.Value;

                        var after = before.Substring(
                            context.Request.PathBase.Value.Length,
                            before.Length - context.Request.PathBase.Value.Length);

                        context.Request.Path = after;
                    }
                }

                return next();
            });
            app.UseSwagger(settings =>
            {
                settings.PostProcess = (document, req) =>
                {
                    document.Info.Version = "v1";
                    document.Info.Title = "Anyways Transit API";
                    document.Info.Description = Documentation.ApiDocs;
                    document.Info.TermsOfService = "None";
                    document.Info.Contact = new SwaggerContact
                    {
                        Name = "Anyways",
                        Email = "info@anyways.eu",
                        Url = "https://www.anyways.eu"
                    };
                    document.BasePath = req.PathBase;
                    document.Host = req.Host.Value;
                };
            });
            app.UseSwaggerUi3(config => config.TransformToExternalPath = (internalUiRoute, request) =>
            {
                // The header X-External-Path is set in the nginx.conf file
                var externalPath = request.PathBase.Value;
                if (externalPath != null && externalPath.EndsWith("/"))
                {
                    externalPath = externalPath.Substring(0, externalPath.Length - 1);
                }

                if (!internalUiRoute.StartsWith(externalPath))
                {
                    return externalPath + internalUiRoute;
                }

                return internalUiRoute;
            });
            app.UseMvc();
            app.UseCors("AllowAllOrigins");
        }

        private static void ConfigureLogging()
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logFile = Path.Combine("logs", $"log-itinero-{date}.txt");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
//.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(new JsonFormatter(), logFile)
                .WriteTo.Console()
                .CreateLogger();
            Log.Information($"Logging has started. Logfile can be found at {logFile}");
            Logger.LogAction = (o, level, message, parameters) =>
            {
                if (string.Equals(level, TraceEventType.Error.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    Log.Error($"{message}");
                }
                else if (string.Equals(level, TraceEventType.Warning.ToString(),
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    Log.Warning($"{message}");
                }
                else if (string.Equals(level, TraceEventType.Information.ToString(),
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    Log.Information($"{message}");
                }
                else if (string.Equals(level, TraceEventType.Verbose.ToString(),
                    StringComparison.CurrentCultureIgnoreCase))
                {
                    Log.Verbose($"{message}");
                }
                else
                {
                    Log.Information($"{level} (unknown log level): {message}");
                }
            };
        }
    }
}