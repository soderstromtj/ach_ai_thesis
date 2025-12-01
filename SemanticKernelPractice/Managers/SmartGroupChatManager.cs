using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticKernelPractice.Managers
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class SmartGroupChatManager : GroupChatManager
    {
        // Count how many times we have selected an agent (a rough "turn" count).
        private int _turnCount = 0;

        /// <summary>
        /// Select a single final result string from the conversation.
        /// In this ACH scenario, we prefer the last message that looks like
        /// the consolidated Evidence JSON, falling back to the last message.
        /// </summary>
        public override ValueTask<GroupChatManagerResult<string>> FilterResults(
            ChatHistory history,
            CancellationToken cancellationToken = default)
        {
            // Prefer the last message that looks like an Evidence JSON object
            // (contains "Evidence" and a '{' character), scanning from the end.
            string? selected = null;

            foreach (var message in history.Reverse())
            {
                var content = message?.Content;
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                var trimmed = content.TrimStart();
                if (trimmed.StartsWith("{", StringComparison.Ordinal) &&
                    trimmed.Contains("\"Evidence\"", StringComparison.OrdinalIgnoreCase))
                {
                    selected = content;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(selected))
            {
                // Fallback: use the last message in the history.
                selected = history.LastOrDefault()?.Content ?? "No evidence JSON produced.";
                return ValueTask.FromResult(
                    new GroupChatManagerResult<string>(selected)
                    {
                        Reason = "No explicit Evidence JSON found; returning the last message as the result."
                    });
            }

            return ValueTask.FromResult(
                new GroupChatManagerResult<string>(selected)
                {
                    Reason = "Returning the last message that appears to be the consolidated Evidence JSON."
                });
        }

        /// <summary>
        /// Decide which agent should speak next.
        /// The flow is:
        ///   1) Facilitator: frame the ACH evidence-extraction task.
        ///   2) TextualForensicAnalyst: extract candidate evidence from the context.
        ///   3) DomainSubjectMatterExpert: add/clarify domain-relevant evidence.
        ///   4) AssumptionAndBiasAuditor: surface and label assumptions.
        ///   5) Contrarian: challenge completeness and neutrality.
        ///   6) Back to Facilitator: integrate and decide whether more passes are needed.
        /// After that, the cycle repeats (2–6) until the Facilitator decides to stop.
        /// </summary>
        public override ValueTask<GroupChatManagerResult<string>> SelectNextAgent(
            ChatHistory history,
            GroupChatTeam team,
            CancellationToken cancellationToken = default)
        {
            _turnCount++;

            string nextAgentName;
            string reason;

            if (_turnCount == 1)
            {
                nextAgentName = "Facilitator";
                reason = "Facilitator starts by framing the ACH evidence extraction task.";
            }
            else
            {
                var lastMessage = history.LastOrDefault();
                var lastAuthor = lastMessage?.AuthorName;

                switch (lastAuthor)
                {
                    case "Facilitator":
                        nextAgentName = "TextualForensicAnalyst";
                        reason = "After the Facilitator frames the task, the Textual Forensic Analyst extracts candidate evidence from the context.";
                        break;

                    case "TextualForensicAnalyst":
                        nextAgentName = "DomainSubjectMatterExpert";
                        reason = "The Domain SME reviews and augments the raw evidence with domain-relevant items.";
                        break;

                    case "DomainSubjectMatterExpert":
                        nextAgentName = "AssumptionAndBiasAuditor";
                        reason = "The Assumption and Bias Auditor surfaces hidden assumptions and labels them explicitly.";
                        break;

                    case "AssumptionAndBiasAuditor":
                        nextAgentName = "Contrarian";
                        reason = "The Contrarian challenges the completeness and neutrality of the current evidence list.";
                        break;

                    default:
                        // After the Contrarian (or any unexpected last author),
                        // return control to the Facilitator to integrate and check for completion.
                        nextAgentName = "Facilitator";
                        reason = "The Facilitator integrates inputs, updates the Evidence JSON, and decides whether further refinement is needed.";
                        break;
                }
            }

            // The team dictionary keys are assumed to match the agent names used above.
            var agentId = team.FirstOrDefault(kvp => kvp.Key == nextAgentName).Key;
            if (string.IsNullOrEmpty(agentId))
            {
                throw new InvalidOperationException($"Agent '{nextAgentName}' not found in the team. Ensure the agent name matches the team key.");
            }

            return ValueTask.FromResult(
                new GroupChatManagerResult<string>(agentId)
                {
                    Reason = reason
                });
        }

        /// <summary>
        /// No user input is required in this ACH evidence-extraction workflow.
        /// The agents run autonomously until the Facilitator decides the evidence list is complete.
        /// </summary>
        public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(
            ChatHistory history,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                new GroupChatManagerResult<bool>(false)
                {
                    Reason = "Automated ACH evidence extraction workflow - no user input needed."
                });
        }

        /// <summary>
        /// Decide whether the group chat should terminate.
        /// We stop when:
        ///   - The base logic says to terminate, OR
        ///   - The Facilitator explicitly declares the Evidence JSON complete/locked, OR
        ///   - A safety upper bound on turns is exceeded.
        /// </summary>
        public override ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(
            ChatHistory history,
            CancellationToken cancellationToken = default)
        {
            // Respect any built-in termination logic first.
            var baseResult = base.ShouldTerminate(history, cancellationToken).Result;
            if (baseResult.Value)
            {
                return ValueTask.FromResult(baseResult);
            }

            // Safety stop: prevent unbounded looping.
            if (_turnCount > 50)
            {
                return ValueTask.FromResult(
                    new GroupChatManagerResult<bool>(true)
                    {
                        Reason = "Safety stop: maximum number of turns reached while extracting evidence."
                    });
            }

            var lastMessage = history.LastOrDefault();
            if (lastMessage?.AuthorName == "Facilitator")
            {
                var content = (lastMessage.Content ?? string.Empty).ToLowerInvariant();

                // The Facilitator is responsible for deciding when the evidence list is sufficient
                // and the Evidence JSON is final. We look for simple textual cues.
                if (content.Contains("evidence json is locked") ||
                    content.Contains("evidence list is complete") ||
                    content.Contains("ach evidence extraction complete") ||
                    content.Contains("final evidence json"))
                {
                    return ValueTask.FromResult(
                        new GroupChatManagerResult<bool>(true)
                        {
                            Reason = "Facilitator declared the Evidence JSON complete and locked."
                        });
                }
            }

            return ValueTask.FromResult(
                new GroupChatManagerResult<bool>(false)
                {
                    Reason = "Continue conversation to further refine the ACH Evidence list."
                });
        }
    }
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
