﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Itinero.Profiles.Lua.Osm;
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

        private IConfiguration Configuration { get; }


        private void StartLoadingTransitDbs()
        {

            OtherModeBuilder otherModeBuilder;

            try
            {
                otherModeBuilder = new OtherModeBuilder(Configuration.GetSection("OsmProfiles"));
            }
            catch (Exception e)
            {
                Log.Error("Could not create all the other profiles: "+e);
                otherModeBuilder = new OtherModeBuilder(null);
            }
            
            var state = new State(Configuration.CreateTransitDbs(), otherModeBuilder) 
                {FreeMessage = "Loading transitdbs"};
            State.GlobalState = state;

            Log.Information("Loaded tdbs are " +
                            string.Join(", ", state.TransitDbs
                                .Select(kvp => kvp.Key)));

            state.NameIndex = new NameIndexBuilder(new List<string> {"name:nl", "name", "name:fr"})
                .Build(state.GetStopsReader(0));


            Log.Information("Performing initial runs");
            state.FreeMessage = "Running initial data syncs";


            var tasks = new List<Task>();
            foreach (var (name, (_, synchronizer)) in state.TransitDbs)
            {
                var t = Task.Run(() =>
                {
                    try
                    {

                        Log.Information($"Starting initial run of {name}");
                        synchronizer.InitialRun();
                        synchronizer.Start();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Caught exception while running initial data sync: {e.Message}\n{e}");
                    }
                });
                tasks.Add(t);
            }

            Task.WaitAll(tasks.ToArray());


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

            Task.Factory.StartNew(StartLoadingTransitDbs);
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
                    document.Schemes = new List<SwaggerSchema>(new []
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