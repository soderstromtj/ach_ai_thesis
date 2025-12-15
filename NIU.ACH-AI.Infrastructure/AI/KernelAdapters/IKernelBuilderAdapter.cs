using Microsoft.SemanticKernel;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.AI.KernelAdapters
{
    /// <summary>
    /// Adapter interface for building Kernel instances with different AI service providers
    /// </summary>
    public interface IKernelBuilderAdapter
    {
        /// <summary>
        /// Builds and configures a Kernel instance for the specific AI provider
        /// </summary>
        /// <param name="modelIdOverride">Optional model ID to override the default model from provider settings</param>
        Kernel BuildKernel(string? modelIdOverride = null);

        /// <summary>
        /// Gets the provider type this adapter supports
        /// </summary>
        AIServiceProvider SupportedProvider { get; }
    }
}