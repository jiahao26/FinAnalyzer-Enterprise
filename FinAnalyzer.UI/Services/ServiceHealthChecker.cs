using System.Net.Http;

namespace FinAnalyzer.UI.Services;

/// <summary>
/// Result of a service health check.
/// </summary>
public record ServiceHealthResult(string ServiceName, bool IsOnline, string Message);

/// <summary>
/// Utility to check health status of Docker services.
/// </summary>
public class ServiceHealthChecker
{
    private readonly HttpClient _httpClient;

    public ServiceHealthChecker()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    /// <summary>
    /// Check if Qdrant vector database is online.
    /// </summary>
    public async Task<ServiceHealthResult> CheckQdrantAsync(string baseUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/");
            if (response.IsSuccessStatusCode)
            {
                return new ServiceHealthResult("Qdrant", true, "Online");
            }
            return new ServiceHealthResult("Qdrant", false, $"HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return new ServiceHealthResult("Qdrant", false, ex.Message);
        }
        catch (TaskCanceledException)
        {
            return new ServiceHealthResult("Qdrant", false, "Timeout");
        }
    }

    /// <summary>
    /// Check if Ollama LLM server is online.
    /// </summary>
    public async Task<ServiceHealthResult> CheckOllamaAsync(string baseUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/api/version");
            if (response.IsSuccessStatusCode)
            {
                return new ServiceHealthResult("Ollama", true, "Online");
            }
            return new ServiceHealthResult("Ollama", false, $"HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return new ServiceHealthResult("Ollama", false, ex.Message);
        }
        catch (TaskCanceledException)
        {
            return new ServiceHealthResult("Ollama", false, "Timeout");
        }
    }

    /// <summary>
    /// Check if TEI reranker server is online.
    /// </summary>
    public async Task<ServiceHealthResult> CheckTeiAsync(string baseUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{baseUrl.TrimEnd('/')}/health");
            if (response.IsSuccessStatusCode)
            {
                return new ServiceHealthResult("TEI Reranker", true, "Online");
            }
            return new ServiceHealthResult("TEI Reranker", false, $"HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return new ServiceHealthResult("TEI Reranker", false, ex.Message);
        }
        catch (TaskCanceledException)
        {
            return new ServiceHealthResult("TEI Reranker", false, "Timeout");
        }
    }

    /// <summary>
    /// Check all services and return combined results.
    /// </summary>
    public async Task<List<ServiceHealthResult>> CheckAllAsync(string qdrantUrl, string ollamaUrl, string teiUrl)
    {
        var tasks = new[]
        {
            CheckQdrantAsync(qdrantUrl),
            CheckOllamaAsync(ollamaUrl),
            CheckTeiAsync(teiUrl)
        };

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
}
