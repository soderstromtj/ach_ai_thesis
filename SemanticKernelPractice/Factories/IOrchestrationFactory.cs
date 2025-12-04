using SemanticKernelPractice.Models;

namespace SemanticKernelPractice.Factories
{
    public interface IOrchestrationFactory<TResult>
    {
        Task<TResult> ExecuteCoreAsync(OrchestrationPromptInput input, CancellationToken cancellationToken = default);
    }
}
