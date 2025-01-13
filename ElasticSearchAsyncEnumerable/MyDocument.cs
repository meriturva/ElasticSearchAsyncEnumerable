using System;

namespace ElasticsearchAsyncEnumerable
{
    
    public class MyDocument
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = default!;
    }
}
