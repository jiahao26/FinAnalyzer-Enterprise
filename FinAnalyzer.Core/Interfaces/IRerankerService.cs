using System.Collections.Generic;
using System.Threading.Tasks;
using FinAnalyzer.Core.Models;

namespace FinAnalyzer.Core.Interfaces
{
    public interface IRerankerService
    {
        Task<IEnumerable<SearchResult>> RerankAsync(string query, IEnumerable<SearchResult> results, int topN = 5);
    }
}
