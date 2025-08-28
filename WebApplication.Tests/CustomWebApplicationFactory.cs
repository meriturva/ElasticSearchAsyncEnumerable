using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.Elasticsearch;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Elastic.Transport;
using ElasticsearchAsyncEnumerable;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Startup>, IAsyncLifetime
    {
        private readonly ElasticsearchContainer Testcontainer = new ElasticsearchBuilder()
           .WithImage("docker.elastic.co/elasticsearch/elasticsearch:9.1.3")
           .Build();

        public ElasticsearchClient GetClient()
        {
            // Create client just for fixture
            var clientSettings = new ElasticsearchClientSettings(new Uri(Testcontainer.GetConnectionString())).DisableDirectStreaming();
            clientSettings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);

            return new ElasticsearchClient(clientSettings);
        }

        public async ValueTask InitializeAsync()
        {
            await Testcontainer.StartAsync();

            // Create client just for fixture
            var client = this.GetClient();

            // Create index
            var indexCreationResponse = await client.Indices.CreateAsync<MyDocument>("my-index", c => c
                  .Mappings(m => m
                    .Properties(p => p
                        .Keyword(d => d.Id)
                        .Text(d => d.Title)
                        )
                      )
                );

            // Fill with millions of records
            IEnumerable<MyDocument> documents = Enumerable.Range(0, 1_000).Select(i =>

                 new MyDocument
                 {
                     Id = Guid.NewGuid(),
                     Title = $"Document {i}"
                 }
            );

            await client.IndexManyAsync(documents, "my-index");
            
            // Refresh all Indices
            await client.Indices.RefreshAsync(Indices.All);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _ = builder.ConfigureServices(services =>
            {
                services.RemoveAll<ElasticsearchClient>();

                services.AddSingleton((_) =>
                {
                    var clientSettings = new ElasticsearchClientSettings(new Uri(Testcontainer.GetConnectionString())).DisableDirectStreaming();
                    clientSettings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);

                    return new ElasticsearchClient(clientSettings);
                });
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await Testcontainer.DisposeAsync();
        }
    }
}
