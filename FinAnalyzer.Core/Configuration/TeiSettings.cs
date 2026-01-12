namespace FinAnalyzer.Core.Configuration
{
    /// <summary>
    /// Configuration settings for TEI (Text Embeddings Inference) reranker service.
    /// </summary>
    public class TeiSettings
    {
        /// <summary>TEI server base URL.</summary>
        public string BaseUrl { get; set; } = "http://localhost:8080";
    }
}
