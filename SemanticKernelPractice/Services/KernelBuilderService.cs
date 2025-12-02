using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Services.KernelBuilders;

namespace SemanticKernelPractice.Services
{
    public class KernelBuilderService : IKernelBuilderService
    {
        private readonly ExperimentConfiguration _experimentConfig;
        private readonly IEnumerable<IKernelBuilderAdapter> _adapters;
        private readonly IKernelBuilderAdapter _selectedAdapter;

        public AIServiceProvider CurrentProvider => _experimentConfig.Provider;

        public KernelBuilderService(ExperimentConfiguration experimentConfig, IEnumerable<IKernelBuilderAdapter> adapters)
        {
            _experimentConfig = experimentConfig;
            _adapters = adapters;

            // Select the appropriate adapter based on experiment's provider configuration
            _selectedAdapter = _adapters.FirstOrDefault(a => a.SupportedProvider == _experimentConfig.Provider)
                ?? throw new InvalidOperationException($"No adapter found for provider: {_experimentConfig.Provider}");
        }

        public Kernel BuildKernel()
        {
            return _selectedAdapter.BuildKernel();
        }
    }
}