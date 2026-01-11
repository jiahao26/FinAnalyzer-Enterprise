using System.Collections.Generic;
using System.Threading.Tasks;
using FinAnalyzer.Core.Models;

namespace FinAnalyzer.Core.Interfaces
{
    public interface IFileLoader
    {
        IAsyncEnumerable<PageContent> LoadAsync(string filePath);
    }
}
