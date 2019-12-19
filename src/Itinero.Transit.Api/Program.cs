using System.Linq;
using Itinero.Transit.Api.Logic;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Itinero.Transit.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("--killing-allowed"))
            {
                args = args.ToList().GetRange(1, args.Length - 1).ToArray();
                State.KillingAllowed = true;
            }
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}