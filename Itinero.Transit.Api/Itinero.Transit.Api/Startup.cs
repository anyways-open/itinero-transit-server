using System;
using System.IO;
using Itinero.Transit.Api.Logic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;
using NJsonSchema;
using NSwag.AspNetCore;
using System.Reflection;


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
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAnyOrigin",
                    builder => builder.AllowAnyOrigin().AllowAnyHeader().WithMethods("GET"));
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            Log.Information("Adding swagger");
            services.AddSwaggerDocument();

            Log.Information("Setting up the DB for today (test)");
            var sncb = PublicTransportRouter.BelgiumSncb;
            //   route?from=&to=&date=201118&time=1230
            var example = sncb.EarliestArrivalRoute(
                sncb.GetStop("http://irail.be/stations/NMBS/008891009"),
                sncb.GetStop("http://irail.be/stations/NMBS/008813003"),
                DateTime.Now.Date.AddHours(10),
                DateTime.Now.Date.AddHours(14)
            );
            Log.Information(example.ToString());
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

            // Register the Swagger generator and the Swagger UI middlewares
            app.UseSwaggerUi3(settings =>
            {
                settings.GeneratorSettings.DefaultPropertyNameHandling =
                    PropertyNameHandling.CamelCase;
            });

            app.UseHttpsRedirection();
            app.UseMvc();
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
        }
    }
}