using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Transit.Api.Logging;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Api.Logic.Search;
using Itinero.Transit.Api.Logic.Transfers;
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

        private IConfiguration Configuration { get; }


        private void StartLoadingTransitDbs()
        {
            OtherModeBuilder otherModeBuilder;

            string routableTileCache = null;
            if (Configuration["RoutableTilesCache"] != null)
            {
                routableTileCache = Configuration.GetValue<string>("RoutableTilesCache");
            }

            try
            {
                otherModeBuilder = new OtherModeBuilder(routableTileCache,
                    Configuration.GetSection("OsmProfiles"));
            }
            catch (Exception e)
            {
                Log.Error("Could not create all the other profiles: " + e);
                otherModeBuilder = new OtherModeBuilder();
            }


            var fileLogger = new FileLogger("logs");
            var operators = Configuration.LoadOperators();

            var state = new State(
                    operators,
                    otherModeBuilder,
                    otherModeBuilder.RouterDb,
                    fileLogger
                )
                {FreeMessage = "Loading transitdbs"};
            Log.Information("Loaded configuration");
            State.GlobalState = state;

            Log.Information("Loaded tdbs are " +
                            string.Join(", ", state.Operators.GetFullView().Operators
                                .Select(kvp => kvp.Name)));

            state.NameIndex = new NameIndexBuilder(new List<string> {"name:nl", "name", "name:fr"})
                .Build(state.Operators.GetFullView().GetStopsReader());


            Log.Information("Performing initial runs");
            state.FreeMessage = "Running initial data syncs";

            var allOperators = state.Operators.GetFullView();

            foreach (var provider in allOperators.Operators)
            {
                try
                {
                    Log.Information($"Starting initial run of {provider.Name}");
                    provider.Synchronizer.InitialRun();
                }
                catch (Exception e)
                {
                    Log.Error($"Caught exception while running initial data sync: {e.Message}\n{e}");
                }
            }

            foreach (var provider in allOperators.Operators)
            {
                try
                {
                    Log.Information($"Starting synchronizer for {provider.Name}");
                    provider.Synchronizer.Start();
                }
                catch (Exception e)
                {
                    Log.Error($"Caught exception while running initial data sync: {e.Message}\n{e}");
                }
            }

            state.FreeMessage = "Fully operational";
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureLogging();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyOrigin",
                    builder => builder.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET"));
            });

            services.AddSwaggerDocument();

            Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        StartLoadingTransitDbs();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"PANIC: could not properly boot the server!\n{e.Message}\n{e.StackTrace}");
                    }
                }
            );
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
                    document.Info.Description = "Welcome to documentation of the Anyways BVBA Transit API.<br />\n" +
                                                "The API offers routing over various public transport networks, such as train, bus, metro or tram networks." +
                                                "The heavy lifting of the routing is done by the <a href='https://github.com/openplannerteam/itinero-transit'>Itinero-transit</a> library, which is built to use <a href='https://linkedconnections.org/'>Linked Connections</a>.<br />\n" +
                                                "<br />" + "<h1> Usage of the API</h1>" +
                                                "<p>To query for a route using public transport, the API can be freely queried via HTTP. To do this, one must first obtain the identifiers of the departure and arrival stations. In the linked data philosophy, an identifier is an URL referring to a station. To obtain a station, one can either use the <code>LocationsAround</code> or <code>LocationsByName</code> endpoints. Please see the swagger documentation for more details.<p>" +
                                                "<p>Once that the locations are known, a journey over the PT-network can be obtained via the <code>Journey</code>-endpoint. Again, see the swagger file for a detailed explanation." +
                                                "" + "<h1>Status of the live server</h1>" +
                                                "<p>The demo endpoint only loads a few operators over a certain timespan. To see which operators are loaded during what time, consult the <code>status</code> endpoint.</p>" +
                                                "" + "<h1>Deploying the server</h1>" + "" +
                                                "<p>To deploy your own Transit-server, use <code>appsettings.json</code> to specify the connection URLs of the operator which you want to load. An example can be found <a href='https://github.com/anyways-open/itinero-transit-server/blob/master/src/Itinero.Transit.Api/appsettings.json'>here</a></p>";
                    document.Info.TermsOfService = "None";
                    document.Info.Contact = new SwaggerContact
                    {
                        Name = "Anyways",
                        Email = "info@anyways.eu",
                        Url = "https://www.anyways.eu"
                    };
                    document.BasePath = req.PathBase;
                    document.Host = req.Host.Value;
                    document.Schemes = new List<SwaggerSchema>(new[]
                    {
                        SwaggerSchema.Https
                    });
#if DEBUG
                    document.Schemes = new List<SwaggerSchema>(new[]
                    {
                        SwaggerSchema.Http
                    });
#endif
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

        public static void ConfigureLogging()
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logFile = Path.Combine("logs", $"log-itinero-{date}.txt");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
.MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Verbose)
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