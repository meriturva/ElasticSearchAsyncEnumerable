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
            //.WithImage("elasticsearch:8.16.1")
            .WithImage("docker.elastic.co/elasticsearch/elasticsearch:8.17.0")
            .Build();

        public ElasticsearchClient GetClient()
        {
            // Create client just for fixture
            var ddd = this.GetConnectionString();
            var clientSettings = new ElasticsearchClientSettings(new Uri(this.GetConnectionString())).DisableDirectStreaming();
            clientSettings.ServerCertificateValidationCallback(CertificateValidations.AllowAll);

            return new ElasticsearchClient(clientSettings);
        }

        public string GetConnectionString()
        {
            return _elasticsearchContainer.GetConnectionString();
        }

        public virtual async Task InitializeAsync()
        {
            await _elasticsearchContainer.StartAsync();
        }

        public virtual async Task DisposeAsync()
        {
            await _elasticsearchContainer.DisposeAsync();
        }
    }
}
