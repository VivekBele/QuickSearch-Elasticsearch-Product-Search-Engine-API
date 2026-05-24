using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using QuickSearch.Api;
using QuickSearch.LoggerUtility;

namespace QuickSearch.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Register LoggerUtility directly
            LoggerStartup.ConfigureServices(builder.Services, builder.Configuration);

            // Configure Elasticsearch client
            var settings = new ElasticsearchClientSettings(new Uri("https://localhost:9200")).CertificateFingerprint("7f605a5dbb5b52b89576cdc87478e4d002f93c2d78512c8dbec3106b2164d45e")
                .Authentication(new BasicAuthentication("elastic", "pl74q6CDn4dDclnD-0R8"));

            var client = new ElasticsearchClient(settings);
            builder.Services.AddSingleton(client);

            // Add services to the container.
            ApiStartup.ConfigureServices(builder.Services, builder.Configuration);

            builder.Services.AddControllers();

            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
