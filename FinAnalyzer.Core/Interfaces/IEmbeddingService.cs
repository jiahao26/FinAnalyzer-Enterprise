using System;
using System.Threading.Tasks;

namespace FinAnalyzer.Core.Interfaces
{
    public interface IEmbeddingService
    {
        Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text);
    }
}
