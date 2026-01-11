using System.Threading.Tasks;

namespace FinAnalyzer.Core.Interfaces
{
    /// <summary>
    /// Represent service managing heavy AI model requiring warm-up.
    /// </summary>
    public interface IModelLifecycle
    {
        /// <summary>
        /// Trigger non-blocking request to force model into memory.
        /// </summary>
        Task WarmUpAsync();
    }
}
