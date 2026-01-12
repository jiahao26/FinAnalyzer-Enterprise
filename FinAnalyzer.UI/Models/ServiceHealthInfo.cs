namespace FinAnalyzer.UI.Models;

/// <summary>
/// Represents the health status of a backend service.
/// </summary>
public sealed class ServiceHealthInfo
{
    public required string ServiceName { get; init; }
    public required string DisplayName { get; init; }
    public required ServiceStatus Status { get; init; }
    public double HealthPercentage { get; init; } = 100;
}

/// <summary>
/// Status of a backend service.
/// </summary>
public enum ServiceStatus
{
    Online,
    Busy,
    Offline,
    Unknown
}
