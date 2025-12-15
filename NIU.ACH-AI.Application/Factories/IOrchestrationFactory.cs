using NIU.ACHAI.Application.Models;

namespace NIU.ACHAI.Application.Factories
{
    public interface IOrchestrationFactory<TResult>
    {
        Task<TResult> ExecuteCoreAsync(OrchestrationPromptInput input, CancellationToken cancellationToken = default);
    }
}
