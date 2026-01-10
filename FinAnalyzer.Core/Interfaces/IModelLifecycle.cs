using System.Threading.Tasks;

namespace FinAnalyzer.Core.Interfaces
{
    /// <summary>
    /// Represents a service that manages a heavy AI model which may require warm-up.
    /// </summary>
    public interface IModelLifecycle
    {
        /// <summary>
        /// Triggers a non-blocking request to the underlying model to force it into memory.
        /// </summary>
        Task WarmUpAsync();
    }
}
