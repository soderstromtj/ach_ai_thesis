using System;
using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.FrontendConsole.Configuration
{
    /// <summary>
    /// Validates the configuration of experiments to ensure strict adherence to required fields.
    /// </summary>
    public static class ExperimentConfigurationValidator
    {
        /// <summary>
        /// Validates that the experiment configuration has all required properties set.
        /// </summary>
        /// <param name="config">The experiment configuration to validate.</param>
        /// <exception cref="InvalidOperationException">Thrown when a required configuration field is missing or empty.</exception>
        public static void Validate(ExperimentConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Id))
            {
                throw new InvalidOperationException("Id is not configured for this experiment. Please add 'Id' to the experiment settings in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(config.Name))
            {
                throw new InvalidOperationException("Name is not configured for this experiment. Please add 'Name' to the experiment settings in appsettings.json.");
            }

            if (string.IsNullOrWhiteSpace(config.Description))
            {
                throw new InvalidOperationException("Description is not configured for this experiment. Please add 'Description' to the experiment settings in appsettings.json.");
            }

            if (config.ACHSteps == null || config.ACHSteps.Length == 0)
            {
                throw new InvalidOperationException("No ACH steps are configured for this experiment. Please add at least one ACH step to the experiment settings in appsettings.json.");
            }
        }
    }
}
