using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;

namespace SemanticKernelPractice.Services.KernelBuilders
{
    /// <summary>
    /// Adapter interface for building Kernel instances with different AI service providers
    /// </summary>
    public interface IKernelBuilderAdapter
    {
        /// <summary>
        /// Builds and configures a Kernel instance for the specific AI provider
        /// </summary>
        Kernel BuildKernel();

        /// <summary>
        /// Gets the provider type this adapter supports
        /// </summary>
        AIServiceProvider SupportedProvider { get; }
    }
}