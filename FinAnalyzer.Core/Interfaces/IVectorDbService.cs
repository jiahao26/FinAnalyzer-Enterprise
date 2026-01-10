using System.Collections.Generic;
using System.Threading.Tasks;
using FinAnalyzer.Core.Models;

namespace FinAnalyzer.Core.Interfaces
{
    public interface IVectorDbService
    {
        Task UpsertAsync(string collectionName, IEnumerable<DocumentChunk> chunks);
        Task<IEnumerable<SearchResult>> SearchAsync(string collectionName, string query, int limit = 10);
    }
}
