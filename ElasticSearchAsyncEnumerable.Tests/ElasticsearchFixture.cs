using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System;
using System.Threading.Tasks;
using Testcontainers.Elasticsearch;
using Xunit;

namespace ElasticSearchAsyncEnumerable.Tests
{
    public class ElasticsearchFixture : IAsyncLifetime
    {
        private readonly ElasticsearchContainer _elasticsearchContainer = new ElasticsearchBuilder()
            .WithImage("docker.elastic.co/elasticsearch/elasticsearch:9.1.3")
            .Build();

        public ElasticsearchClient GetClient()
        {
            // Create client just for fixture
            var clientSettings = new ElasticsearchClientSettings(new Uri(this.GetConnectionString())).DisableDirectStreaming();
            clientSettings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);

            return new ElasticsearchClient(clientSettings);
        }

        public string GetConnectionString()
        {
            return _elasticsearchContainer.GetConnectionString();
        }

        public virtual async ValueTask InitializeAsync()
        {
            await _elasticsearchContainer.StartAsync();
        }

        public virtual async ValueTask DisposeAsync()
        {
            await _elasticsearchContainer.DisposeAsync();
        }
    }
}
