using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Interfaces
{
    /// <summary>
    /// Interface for extracting token usage information from AI service responses.
    /// </summary>
    public interface ITokenUsageExtractor
    {
        /// <summary>
        /// Extracts token usage information from the metadata of a chat completion response.
        /// </summary>
        /// <param name="metadata">The metadata dictionary from the AI service response.</param>
        /// <returns>A structured object containing token usage details.</returns>
        TokenUsageInfo ExtractTokenUsage(IReadOnlyDictionary<string, object?>? metadata);
    }
}
