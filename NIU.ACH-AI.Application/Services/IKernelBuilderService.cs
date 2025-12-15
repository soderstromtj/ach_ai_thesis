using Microsoft.SemanticKernel;
using NIU.ACHAI.Application.Configuration;

namespace NIU.ACHAI.Application.Services
{
    /// <summary>
    /// Service responsible for building Kernel instances based on configured AI provider
    /// </summary>
    public interface IKernelBuilderService
    {
        /// <summary>
        /// Builds a Kernel instance using the configured AI service provider
        /// </summary>
        Kernel BuildKernel();

        /// <summary>
        /// Gets the currently configured AI service provider
        /// </summary>
        AIServiceProvider CurrentProvider { get; }
    }
}
