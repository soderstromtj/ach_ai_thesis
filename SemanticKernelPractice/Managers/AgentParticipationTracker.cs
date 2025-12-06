using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticKernelPractice.Managers
{
    /// <summary>
    /// Tracks agent participation in group chat conversations.
    /// </summary>
    public class AgentParticipationTracker
    {
        /// <summary>
        /// Determines if all expected agents have participated at least once in the conversation.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <param name="expectedAgentNames">The list of expected agent names.</param>
        /// <returns>True if all agents have participated; otherwise, false.</returns>
        public bool HaveAllAgentsParticipated(ChatHistory history, List<string> expectedAgentNames)
        {
            ArgumentNullException.ThrowIfNull(history);
            ArgumentNullException.ThrowIfNull(expectedAgentNames);

            var participatingAgents = GetParticipatingAgents(history);
            return participatingAgents.Count >= expectedAgentNames.Count;
        }

        /// <summary>
        /// Gets the set of unique agent names that have participated in the conversation.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <returns>A set of agent names that have contributed to the conversation.</returns>
        public HashSet<string> GetParticipatingAgents(ChatHistory history)
        {
            ArgumentNullException.ThrowIfNull(history);

            return history
                .Where(msg => msg.Role == AuthorRole.Assistant)
                .Select(msg => msg.AuthorName ?? string.Empty)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToHashSet();
        }

        /// <summary>
        /// Gets the list of agents that have not yet participated in the conversation.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <param name="expectedAgentNames">The list of expected agent names.</param>
        /// <returns>A list of agent names that have not yet contributed.</returns>
        public List<string> GetNonParticipatingAgents(ChatHistory history, List<string> expectedAgentNames)
        {
            ArgumentNullException.ThrowIfNull(history);
            ArgumentNullException.ThrowIfNull(expectedAgentNames);

            var participatingAgents = GetParticipatingAgents(history);
            return expectedAgentNames.Where(name => !participatingAgents.Contains(name)).ToList();
        }
    }
}
