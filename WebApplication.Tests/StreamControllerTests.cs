using ElasticsearchAsyncEnumerable;
using FluentAssertions;
using System.Text.Json;

namespace WebApplication.Tests
{
    public class StreamControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _fixture;
        private readonly HttpClient _client;

        public StreamControllerTests(CustomWebApplicationFactory fixture)
        {
            _fixture = fixture;
            _client = _fixture.CreateClient();
        }

        [Fact]
        public async Task TestStream()
        {
            // Act
            var response = await _client.GetAsync("stream");

            // Assert
            response.EnsureSuccessStatusCode();
            response.Content.Headers.ContentType.MediaType.Should().Be("application/x-ndjson");

            // Get content as string
            var responseStr = await response.Content.ReadAsStringAsync();
            var responseSingleLineStr = responseStr.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            responseSingleLineStr.Should().HaveCount(1000);

            // Deserialize content
            var documents = responseSingleLineStr.Select(line => JsonSerializer.Deserialize<MyDocument>(line));
            documents.Should().HaveCount(1000);
        }
    }
}
