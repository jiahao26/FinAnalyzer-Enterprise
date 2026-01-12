namespace FinAnalyzer.UI.Models;

/// <summary>
/// Represents a step in the RAG processing pipeline.
/// </summary>
public sealed class PipelineStep
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required PipelineStepStatus Status { get; init; }
    public required string Icon { get; init; }
}

/// <summary>
/// Status of a pipeline step.
/// </summary>
public enum PipelineStepStatus
{
    Done,
    Processing,
    Pending
}
