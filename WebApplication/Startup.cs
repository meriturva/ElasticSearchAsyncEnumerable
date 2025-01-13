using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using ElasticsearchAsyncEnumerable;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json.Serialization;

namespace WebApplication
{
    public class Startup
    {
        private IConfiguration _configuration { get; }

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Get connection string of elasticsearch from configuration
            services.AddSingleton((_) =>
            {
                var esConnectionString = _configuration.GetValue<string>("ElasticSearchConnectionString")!;
                var clientSettings = new ElasticsearchClientSettings(new Uri(esConnectionString)).DisableDirectStreaming();
                clientSettings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);

                return new ElasticsearchClient(clientSettings);
            });

            services.AddScoped<MyIndexRepository>();

            services.AddControllersWithViews()
                .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
