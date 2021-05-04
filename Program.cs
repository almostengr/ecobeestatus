using Almostengr.InternetMonitor.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Almostengr.EcobeeStatus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration configuration = hostContext.Configuration;
                    AppSettings appSettings = configuration.GetSection(nameof(appSettings)).Get<AppSettings>();
                    services.AddSingleton(appSettings);
                    
                    services.AddHostedService<Worker>();
                });
    }
}
