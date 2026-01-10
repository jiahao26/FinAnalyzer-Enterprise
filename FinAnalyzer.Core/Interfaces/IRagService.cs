using System;
using System.Collections.Generic;
using System.Text;

namespace FinAnalyzer.Core.Interfaces
{
    public interface IRagService
    {
        Task IngestDocumentAsync(string filePath);
        Task<string> QueryAsync(string question);
    }
}
