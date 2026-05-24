using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuickSearch.Model;

namespace QuickSearch.Api
{
    public static class ApiStartup
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register controllers from this assembly
            services.AddControllers().AddApplicationPart(typeof(ApiStartup).Assembly);

            // Bind Elasticsearch section from appsettings.json
            services.Configure<ElasticsearchOptionsConfigurations>(
                configuration.GetSection("Elasticsearch"));

            // Register ProductServices with DI
            services.AddScoped<IProductServices, ProductServices>();
        }
    }
}
