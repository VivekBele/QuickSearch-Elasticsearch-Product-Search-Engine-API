using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuickSearch.Data;
using QuickSearch.Data.Repositories;
using QuickSearch.DataAccess;
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

            // Register EF Core DbContext
            string sqlConnectionString = configuration.GetConnectionString("SqlDb") ?? 
                "Server=localhost;Database=QuickSearch;Trusted_Connection=True;TrustServerCertificate=True;";
            services.AddDbContext<QuickSearchDbContext>(options =>
                options.UseSqlServer(sqlConnectionString));

            // Register generic repository
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            // Register DataAccess Services
            services.AddScoped<IProductDbService, ProductDbService>();
            services.AddScoped<IUserDbService, UserDbService>();

            // Register ProductServices with DI
            services.AddScoped<IProductServices, ProductServices>();
        }
    }
}
