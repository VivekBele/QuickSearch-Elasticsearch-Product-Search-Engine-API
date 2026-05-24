using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QuickSearch.LoggerUtility
{
    public static class LoggerStartup
    {
        public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Read MongoDB connection settings from appsettings.json
            var connectionString = configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017";
            var databaseName = configuration["Logger:Database"] ?? "QuickSearchLogs";
            var collectionName = configuration["Logger:Collection"] ?? "ApiLogs";

            // Register Logger as ILogger
            services.AddSingleton<ILogger>(new Logger(connectionString, databaseName, collectionName));
        }
    }
}
