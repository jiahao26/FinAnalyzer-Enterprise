namespace FinAnalyzer.Core.Configuration
{
    /// <summary>
    /// Configuration settings for Qdrant vector database connection.
    /// </summary>
    public class QdrantSettings
    {
        /// <summary>Qdrant server hostname.</summary>
        public string Host { get; set; } = "localhost";
        
        /// <summary>Qdrant gRPC port (default: 6334).</summary>
        public int Port { get; set; } = 6334;
        
        /// <summary>Qdrant HTTP/REST port (default: 6333).</summary>
        public int HttpPort { get; set; } = 6333;
        
        /// <summary>Embedding vector dimension size.</summary>
        public int VectorSize { get; set; } = 768;
    }
}
