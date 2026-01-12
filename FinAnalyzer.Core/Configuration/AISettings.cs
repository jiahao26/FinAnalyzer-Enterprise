namespace FinAnalyzer.Core.Configuration
{
    public enum AIBackendType
    {
        Ollama,
        OpenAI_Compatible
    }

    public class AISettings
    {
        public string BackendType { get; set; } = "Ollama";
        public string ChatEndpoint { get; set; } = "http://localhost:11434";
        public string ChatModelId { get; set; } = "llama3:8b-instruct-q8_0";
        public string EmbeddingEndpoint { get; set; } = "http://localhost:11434";
        public string EmbeddingModelId { get; set; } = "nomic-embed-text";
        public string? ApiKey { get; set; }
    }
}
