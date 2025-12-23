using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.Persistence.Entities;

namespace NIU.ACH_AI.Infrastructure.Persistence.Mappers
{
    /// <summary>
    /// Maps between domain Hypothesis and database HypothesisEntity
    /// </summary>
    public static class HypothesisMapper
    {
        /// <summary>
        /// Converts a domain Hypothesis to a database HypothesisEntity
        /// </summary>
        /// <param name="hypothesis">The domain hypothesis to convert</param>
        /// <param name="stepExecutionId">The step execution ID that generated this hypothesis</param>
        /// <param name="isRefined">Whether the hypothesis has been refined</param>
        /// <returns>A database entity ready to be persisted</returns>
        public static HypothesisEntity ToDatabase(
            Hypothesis hypothesis,
            Guid stepExecutionId,
            bool isRefined)
        {
            if (hypothesis == null)
            {
                throw new ArgumentNullException(nameof(hypothesis));
            }

            if (stepExecutionId == Guid.Empty)
            {
                throw new ArgumentException("StepExecutionId cannot be empty", nameof(stepExecutionId));
            }

            return new HypothesisEntity
            {
                HypothesisId = Guid.NewGuid(),
                StepExecutionId = stepExecutionId,
                ShortTitle = hypothesis.ShortTitle ?? string.Empty,
                HypothesisText = hypothesis.HypothesisText ?? string.Empty,
                IsRefined = isRefined,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Converts a database HypothesisEntity to a domain Hypothesis
        /// </summary>
        /// <param name="entity">The database entity to convert</param>
        /// <returns>A domain hypothesis</returns>
        public static Hypothesis ToDomain(HypothesisEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            return new Hypothesis
            {
                ShortTitle = entity.ShortTitle,
                HypothesisText = entity.HypothesisText
            };
        }
    }
}
