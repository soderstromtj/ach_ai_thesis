using NIU.ACHAI.Application.Models;

namespace NIU.ACHAI.Application.Services
{
    /// <summary>
    /// Provides a mechanism to select the appropriate orchestration factory based on the ACH step.
    /// </summary>
    public interface IOrchestrationFactorySelector
    {
        /// <summary>
        /// Gets the orchestration factory appropriate for the specified ACH step.
        /// </summary>
        /// <param name="step">The ACH step for which to get the factory.</param>
        /// <returns>The orchestration factory instance for the specified step.</returns>
        /// <exception cref="NotSupportedException">Thrown when the specified ACH step is not supported.</exception>
        object GetFactory(ACHStep step);

        /// <summary>
        /// Gets the result type for the specified ACH step.
        /// </summary>
        /// <param name="step">The ACH step.</param>
        /// <returns>The Type of the result produced by the factory for this step.</returns>
        Type GetResultType(ACHStep step);
    }
}
