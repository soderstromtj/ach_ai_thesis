using Microsoft.SemanticKernel.ChatCompletion;

namespace NIU.ACH_AI.Infrastructure.AI.Managers
{
    /// <summary>
    /// Tracks agent participation in group chat conversations.
    /// </summary>
    public class AgentParticipationTracker
    {
        /// <summary>
        /// Checks if all the expected agents have sent at least one message.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <param name="expectedAgentNames">The list of expected agent names.</param>
        /// <returns>True if all agents have participated; otherwise, false.</returns>
        public bool HaveAllAgentsParticipated(ChatHistory history, IEnumerable<string> expectedAgentNames)
        {
            ArgumentNullException.ThrowIfNull(history);
            ArgumentNullException.ThrowIfNull(expectedAgentNames);

            var participatingAgents = GetParticipatingAgents(history);
            return expectedAgentNames.All(expected => participatingAgents.Contains(expected));
        }

        /// <summary>
        /// Gets the names of all agents who have sent a message.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <returns>A hash set containing the names of all agents that have contributed at least one message.</returns>
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
        /// Gets the names of agents who haven't sent a message yet.
        /// </summary>
        /// <param name="history">The chat history to analyze.</param>
        /// <param name="expectedAgentNames">The list of expected agent names.</param>
        /// <returns>A list of agent names that have not yet contributed.</returns>
        public List<string> GetNonParticipatingAgents(ChatHistory history, IEnumerable<string> expectedAgentNames)
        {
            ArgumentNullException.ThrowIfNull(history);
            ArgumentNullException.ThrowIfNull(expectedAgentNames);

            var participatingAgents = GetParticipatingAgents(history);
            return expectedAgentNames.Where(name => !participatingAgents.Contains(name)).ToList();
        }
    }
}
