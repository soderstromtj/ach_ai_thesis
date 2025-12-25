using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Interface for creating orchestration factories based on ACH step configuration.
    /// </summary>
    public interface IOrchestrationFactoryProvider
    {
        /// <summary>
        /// Creates the appropriate orchestration factory for the given ACH step configuration.
        /// </summary>
        /// <typeparam name="TResult">The expected result type from the factory</typeparam>
        /// <param name="stepConfiguration">The ACH step configuration</param>
        /// <returns>An orchestration factory that produces results of type TResult</returns>
        IOrchestrationFactory<TResult> CreateFactory<TResult>(ACHStepConfiguration stepConfiguration);
    }
}
