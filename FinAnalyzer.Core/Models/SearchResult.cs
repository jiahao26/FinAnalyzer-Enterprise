namespace FinAnalyzer.Core.Models
{
    public class SearchResult
    {
        public DocumentChunk Chunk { get; set; } = new DocumentChunk();
        public double Score { get; set; }
    }
}
