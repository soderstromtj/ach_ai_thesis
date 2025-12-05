using Microsoft.SemanticKernel;
using SemanticKernelPractice.Configuration;
using SemanticKernelPractice.Services.KernelBuilders;

namespace SemanticKernelPractice.Services
{
    public class KernelBuilderService : IKernelBuilderService
    {
        private readonly IKernelBuilderAdapter _adapter;

        public AIServiceProvider CurrentProvider => AIServiceProvider.Unified;

        public KernelBuilderService(IKernelBuilderAdapter adapter)
        {
            _adapter = adapter;
        }

        public Kernel BuildKernel()
        {
            return _adapter.BuildKernel();
        }
    }
}