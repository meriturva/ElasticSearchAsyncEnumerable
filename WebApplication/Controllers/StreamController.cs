using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Nodes;
using ElasticsearchAsyncEnumerable;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StreamController : ControllerBase
    {
        private static readonly byte[] newLineBytes = new System.Text.UTF8Encoding().GetBytes(Environment.NewLine);
        private readonly MyIndexRepository _indexRepository;
        private readonly ElasticsearchClient _esClientForFill;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { 
            WriteIndented = false
            };

        public StreamController(MyIndexRepository indexRepository, ElasticsearchClient esClientForFill)
        {
            _indexRepository = indexRepository;
            this._esClientForFill = esClientForFill;
        }

        [HttpPost]
        public async Task<IActionResult> FillAsync(CancellationToken cancellationToken = default)
        {
            // Create index
            var indexCreationResponse = await _esClientForFill.Indices.CreateAsync<MyDocument>("my-index", c => c
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

            var indexResult = await _esClientForFill.IndexManyAsync(documents, "my-index");

            // Refresh all Indices
            await _esClientForFill.Indices.RefreshAsync(Indices.All);

            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(CancellationToken cancellationToken = default)
        {
            this.HttpContext.Response.ContentType = "application/x-ndjson";

            await foreach (var document in _indexRepository.GetDocumentsAsyncEnumerableAsync(cancellationToken))
            {
                var documentJson = JsonSerializer.SerializeToUtf8Bytes(document, _jsonSerializerOptions);
                await this.HttpContext.Response.Body.WriteAsync(documentJson, cancellationToken);
                await this.HttpContext.Response.Body.WriteAsync(newLineBytes, cancellationToken);
                // Just to simulate calculation during data retrive phase
                await Task.Delay(1);
            }

            await this.HttpContext.Response.Body.FlushAsync();

            return new EmptyResult();
        }
    }
}
