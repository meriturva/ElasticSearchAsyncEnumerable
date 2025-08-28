using ElasticsearchAsyncEnumerable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElasticSearchAsyncEnumerable.Tests
{
    public class AsynEnumerableTests : IClassFixture<ElasticsearchMillionsRecordFixture>
    {
        private readonly ElasticsearchMillionsRecordFixture _fixture;
        private readonly MyIndexRepository _myIndexRepository;

        public AsynEnumerableTests(ElasticsearchMillionsRecordFixture fixture)
        {
            _fixture = fixture;
            _myIndexRepository = new MyIndexRepository(_fixture.GetClient());
        }

        [Fact]
        public async Task SimplePingTest()
        {
            // Arrange
            var client = _fixture.GetClient();

            // Act
            var response = await client.PingAsync(TestContext.Current.CancellationToken);

            // Arrange
            Assert.True(response.IsValidResponse);
        }


        [Fact]
        public async Task PaginedResult()
        {
            // Act
            var documents = await _myIndexRepository.GetDocumentsAsync(TestContext.Current.CancellationToken);

            // Arrange
            Assert.True(documents.Count() == 1_000_000);
        }

        [Fact]
        public async Task PaginedResultAsyncEnumerable()
        {
            // Act
            var count = 0;

            await foreach (var document in _myIndexRepository.GetDocumentsAsyncEnumerableAsync(TestContext.Current.CancellationToken))
            {
                count++;
            }

            // Arrange
            Assert.True(count == 1_000_000);
        }
    }
}
