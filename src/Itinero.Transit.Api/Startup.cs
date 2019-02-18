using System;
using System.Collections.Generic;
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

            State.BootTime = DateTime.Now;


            var sources = Configuration.GetSection("Datasources");
            if (!sources.GetChildren().Any())
            {
                throw new ArgumentException(
                    "No datasource is specified in the configuration. Please add at least one operator");
            }

            if (sources.GetChildren().Count() > 1)
            {
                throw new ArgumentException("For this beta version, only one operator is supported");
            }

            // Get first element of the list in datasources
            var source = sources.GetChildren().First();


            Log.Information(
                $"Loading PT operator {source.GetSection("Locations").Value} {source.GetSection("Connections").Value}");

            (State.TransitDb, State.LcProfile) = TransitDbFactory.CreateTransitDb(
                source.GetSection("Connections").Value,
                source.GetSection("Locations").Value);

            State.JourneyTranslator = new JourneyTranslator(State.TransitDb);

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigin",
                    builder => builder.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET"));
            });


            Log.Information("Adding swagger");
            services.AddSwaggerDocument();


            void SampleImportances()
            {
                // Sample importance based on today
                State.Importances = ImportanceCount.CalculateImportance(
                    State.LcProfile, DateTime.Today, DateTime.Today.AddDays(1));
            }

            List<(DateTime start, DateTime end)> CreateWindows(DateTime now)
            {
                var policy = Configuration.GetSection("AutoLoad");
                var timeBefore = policy.GetValue<ulong>("TimeBefore");
                var timeAfter = policy.GetValue<ulong>("TimeAfter");
                var intervalSplit = policy.GetValue<ulong>("IntervalSplit");

                if (intervalSplit == 0)
                {
                    Log.Warning("No interval split given, or is zero");
                    intervalSplit = timeAfter + timeBefore;
                }

                var windows = new List<(DateTime start, DateTime end)>();

                var t = now.ToUnixTime();

                var start = t - timeBefore;
                var end = t + timeAfter;

                var wStart = start;
                ulong wEnd;
                do
                {
                    wEnd = Math.Min(wStart + intervalSplit, end);
                    windows.Add((wStart.FromUnixTime(), wEnd.FromUnixTime()));

                    wStart = wEnd;
                } while (wEnd < end);

                return windows;
            }

            void StartAutoReloads()
            {
                // Loads and reloads the connectionsDB
                var reloadEvery = Configuration.GetSection("AutoLoad").GetValue<ulong>("ReloadEvery");
                new ConnectionAutoLoader(State.TransitDb, TimeSpan.FromSeconds(reloadEvery), CreateWindows);
            }


            Task.Factory.StartNew(StartAutoReloads);
            Task.Factory.StartNew(SampleImportances);
            // TODO Headsigns are gone! Where are they?
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
                    Log.Information(
                        $"Detected path base header, changing {context.Request.PathBase} to {pathBases.First()}");
                    context.Request.PathBase = pathBases.First();
                    if (context.Request.PathBase.Value.EndsWith("/"))
                    {
                        Log.Information($"Removing trailing slash from: {context.Request.PathBase.Value}.");
                        context.Request.PathBase =
                            context.Request.PathBase.Value.Substring(0, context.Request.PathBase.Value.Length - 1);
                        Log.Information($"Removing trailing slash, now: {context.Request.PathBase.Value}.");
                    }

                    if (context.Request.Path.Value.StartsWith(context.Request.PathBase.Value))
                    {
                        var before = context.Request.Path.Value;
                        var after = context.Request.Path.Value.Substring(
                            context.Request.PathBase.Value.Length,
                            context.Request.Path.Value.Length - context.Request.PathBase.Value.Length);
                        Log.Information($"Path changed to: {after}, was {before}.");
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
                    Log.Information($"Set swagger document base path to: {document.BasePath}.");

                    document.Host = req.Host.Value;
                    Log.Information($"Set swagger document host to: {document.Host}.");
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
                    Log.Information($"Configured external path to: {externalPath + internalUiRoute}.");
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
            Logger.LogAction("a", "b", "c", null);
        }
    }
}