using AspNetScaffolding;
using AspNetScaffolding.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace PipelineR.StartingSample
{
    public class Program
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.SetupPipelineR();
        }

        public static void Main(string[] _)
        {
            var config = new ApiBasicConfiguration
            {
                ApiName = "PipelineR Starting Sample",
                ApiPort = 8700,
                EnvironmentVariablesPrefix = "PipelineR_",
                ConfigureServices = ConfigureServices,
                AutoRegisterAssemblies = new Assembly[]
                { Assembly.GetExecutingAssembly() }
            };

            Api.Run(config);
        }
    }
}