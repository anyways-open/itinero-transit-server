using System;
using System.Diagnostics;
using System.IO;
using Itinero.Transit.Api.Controllers;
using Itinero.Transit.Api.Logic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Json;


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

            var db = DatabaseLoader.Belgium();
            var tr = new JourneyTranslator(db);
            LocationController.Translator = tr;
            LocationsAroundController.StopsDb = db.Stops;
            JourneyController.Translator = tr;
            JourneyController.Db = db;
            LocationsByNameController.Locations = db.Profile;
            LocationsByNameController.Importances = db.ConnectionCounts;

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyOrigin",
                    builder => builder.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET"));
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            Log.Information("Adding swagger");
            services.AddSwaggerDocument();
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

            app.UseSwagger();
            app.UseSwaggerUi3();

            app.UseHttpsRedirection();
            app.UseMvc();
            app.UseFileServer();
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

            Itinero.Transit.Logging.Logger.LogAction = (o, level, localmessage, parameters) =>
            {
                if (String.Equals(level, TraceEventType.Error.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    Log.Error($"{localmessage}");
                }
                else if (String.Equals(level, TraceEventType.Warning.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    Log.Warning($"{localmessage}");
                }
                else if (String.Equals(level, TraceEventType.Information.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    Log.Information($"{localmessage}");
                }
                else if (String.Equals(level, TraceEventType.Verbose.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    Log.Verbose($"{localmessage}");
                }
                else
                {
                    Log.Information($"{level} (unknown log level): {localmessage}");
                }
            };
            Itinero.Transit.Logging.Logger.LogAction("a", "b", "c", null);
        }
    }
}