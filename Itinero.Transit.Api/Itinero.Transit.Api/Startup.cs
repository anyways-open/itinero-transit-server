using System;
using System.IO;
using Itinero.Transit.Api.Controllers;
using Itinero.Transit.Api.Logic;
using Itinero.Transit.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;
using NJsonSchema;


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
            StopsController.StopsDb = db.Stops;
            
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
            app.UseSwaggerUi3(settings =>
            {
                settings.GeneratorSettings.DefaultPropertyNameHandling =
                    PropertyNameHandling.CamelCase;
                
                settings.PostProcess = document =>
                {
                    document.Info.Version = "v1";
                    document.Info.Title = "Public Transport routing";
                    document.Info.Description = "A simple ASP.NET Core web API";
                    document.Info.TermsOfService = "None";
                    document.Info.Contact = new NSwag.SwaggerContact
                    {
                        Name = "Pieter Vander Vennet",
                        Email = "pieter@anyways.eu",
                        Url = "anyways.eu"
                    };
                    document.Info.License = new NSwag.SwaggerLicense
                    {
                        Name = "MIT",
                    };
                };
                
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