using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Services.KernelBuilders;

namespace SemanticKernelPractice.Services
{
    public class KernelBuilderService : IKernelBuilderService
    {
        private readonly AIServiceSettings _serviceSettings;
        private readonly IEnumerable<IKernelBuilderAdapter> _adapters;
        private readonly IKernelBuilderAdapter _selectedAdapter;

        public AIServiceProvider CurrentProvider => _serviceSettings.Provider;

        public KernelBuilderService(IOptions<AIServiceSettings> serviceSettings, IEnumerable<IKernelBuilderAdapter> adapters)
        {
            _serviceSettings = serviceSettings.Value;
            _adapters = adapters;

            // Select the appropriate adapter based on configuration
            _selectedAdapter = _adapters.FirstOrDefault(a => a.SupportedProvider == _serviceSettings.Provider)
                ?? throw new InvalidOperationException(
                    $"No adapter found for provider: {_serviceSettings.Provider}");
        }

        public Kernel BuildKernel()
        {
            return _selectedAdapter.BuildKernel();
        }
    }
}