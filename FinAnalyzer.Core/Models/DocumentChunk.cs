using System.Collections.Generic;

namespace FinAnalyzer.Core.Models
{
    public class DocumentChunk
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string SourceFileName { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public ReadOnlyMemory<float> Vector { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
