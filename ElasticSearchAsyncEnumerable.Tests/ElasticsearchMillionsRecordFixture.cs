using Elastic.Clients.Elasticsearch;
using ElasticsearchAsyncEnumerable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElasticSearchAsyncEnumerable.Tests
{
    public class ElasticsearchMillionsRecordFixture : ElasticsearchFixture
    {
        public override async ValueTask InitializeAsync()
        {
            await base.InitializeAsync();

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
            IEnumerable<MyDocument> documents = Enumerable.Range(0, 1_000_000).Select(i =>

                 new MyDocument
                 {
                     Id = Guid.NewGuid(),
                     Title = $"Document {i}"
                 }
            );

            foreach (var documentsChunk in documents.Chunk(10_000))
            {
                var indexResult = await client.IndexManyAsync(documentsChunk, "my-index");
            }

            // Refresh all Indices
            await client.Indices.RefreshAsync(Indices.All);
        }
    }
}
