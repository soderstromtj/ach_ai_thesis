using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    public interface IOrchestrationFactory<TResult>
    {
        Task<TResult> ExecuteCoreAsync(OrchestrationPromptInput input, CancellationToken cancellationToken = default);
    }
}
