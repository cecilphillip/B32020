using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace B320
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            IHost applicationHost = CreateHostBuilder(args).Build();
            await applicationHost.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<ITextTransformer, L33tTransformer>();
                    services.AddSingleton<DigitalSigner>(provider => new DigitalSigner("SHA512"));
                    services.AddSingleton<PayloadProcessingChannel>();
                    services.AddHostedService<PayloadGeneratorWorker>();
                    services.AddHostedService<DataAcquisitionWorker>();
                    services.AddHostedService<ProcessHandlerWorker>();
                });
    }
}