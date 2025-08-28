using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticsearchAsyncEnumerable
{
    public class MyIndexRepository
    {
        private readonly ElasticsearchClient _client;
        private string _indexName = "my-index";
        protected string _keepAlive = "1m";
        protected int _maxResult = 10;

        public MyIndexRepository(ElasticsearchClient client)
        {
            this._client = client;
        }

        public async Task<IEnumerable<MyDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default)
        {
            // Create a new pit
            var crationPitResult = await this._client.OpenPointInTimeAsync(_indexName, p => p.KeepAlive(_keepAlive), cancellationToken);

            var lastPitId = crationPitResult.Id;

            List<MyDocument> docs = [];
            ICollection<FieldValue> searchAfter = null;

            while (true)
            {
                var searchResponse = await _client.SearchAsync<MyDocument>(sd => sd.Index(_indexName)
                .Size(_maxResult)
                .TrackTotalHits(new TrackHits(false))
                .Pit(new PointInTimeReferenceDescriptor(lastPitId))
                .SearchAfter(searchAfter)
                .Sort(sd => sd.Field(document => document.Id)), cancellationToken);

                if (searchResponse.Hits.Count == 0)
                {
                    break;
                }

                // Add documents to whole array
                docs.AddRange(searchResponse.Documents);
                searchAfter = searchResponse.HitsMetadata.Hits.Last().Sort.ToList();

                // The open point in time request and each subsequent search request can return different id;
                // thus always use the most recently received id for the next search request.
                lastPitId = searchResponse.PitId;
            }

            // Delete pit
            await _client.ClosePointInTimeAsync(v => new ClosePointInTimeRequest { Id = lastPitId }, cancellationToken);
            return docs;
        }

        public async IAsyncEnumerable<MyDocument> GetDocumentsAsyncEnumerableAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Create a new pit
            var crationPitResult = await this._client.OpenPointInTimeAsync(_indexName, p => p.KeepAlive(_keepAlive), cancellationToken);

            var lastPitId = crationPitResult.Id;
            ICollection<FieldValue> searchAfter = null;

            try
            {
                while (true)
                {
                    // We have to enable pagination
                    var searchResponse = await _client.SearchAsync<MyDocument>(sd => sd.Indices(_indexName)
                    .Size(_maxResult)
                    .TrackTotalHits(new TrackHits(false))
                    .Pit(p => p.Id(lastPitId))
                    .SearchAfter(searchAfter)
                    .Sort(sd => sd.Field(document => document.Id)), cancellationToken);

                    if (searchResponse.Hits.Count == 0)
                    {
                        break;
                    }

                    searchAfter = searchResponse.HitsMetadata.Hits.Last().Sort.ToList();
                    // The open point in time request and each subsequent search request can return different id;
                    // thus always use the most recently received id for the next search request.
                    lastPitId = searchResponse.PitId;

                    foreach (var doc in searchResponse.Documents)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return doc;
                    }
                }
            }
            finally
            {
                // Delete pit
                await _client.ClosePointInTimeAsync(v => new ClosePointInTimeRequest { Id = lastPitId }, cancellationToken);
            }
        }
    }
}
