namespace SemanticKernelPractice.Factories
{
    public interface IOrchestrationFactory<TResult>
    {
        Task<TResult> ExecuteCoreAsync(string input, CancellationToken cancellationToken = default);
    }
}
