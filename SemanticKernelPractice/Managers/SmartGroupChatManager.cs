using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPractice.Managers
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public class SmartGroupChatManager : GroupChatManager
    {
        private int _messageCount = 0;

        public override ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history, CancellationToken cancellationToken = default)
        {
            var lastMessage = history.LastOrDefault()?.Content ?? "No result";
            return ValueTask.FromResult(
                new GroupChatManagerResult<string>(lastMessage)
                {
                    Reason = "Using last message as final result"
                });
        }

        public override ValueTask<GroupChatManagerResult<string>> SelectNextAgent(ChatHistory history, GroupChatTeam team, CancellationToken cancellationToken = default)
        {
            _messageCount++;

            string nextAgentName;
            string reason;

            if (_messageCount == 1)
            {
                nextAgentName = "Marketer";
                reason = "Starting with the marketing strategy.";
            }
            else
            {
                var lastMessage = history.LastOrDefault();
                var lastAgent = lastMessage?.AuthorName;

                if (lastAgent == "Marketer" || lastAgent == "Critic")
                {
                    nextAgentName = "Writer";
                    reason = lastAgent == "Marketer" ? "Writer responds to marketing insights." : "Writer refines based on feedback.";
                }
                else
                {
                    nextAgentName = "Critic";
                    reason = "Critic evaluates the content created by the Writer.";
                }
            }

            var agentId = team.FirstOrDefault(kvp => kvp.Key == nextAgentName).Key;
            if (string.IsNullOrEmpty(agentId))
            {
                throw new InvalidOperationException($"Agent '{nextAgentName}' not found in the team.");
            }

            return ValueTask.FromResult(
                new GroupChatManagerResult<string>(agentId)
                {
                    Reason = reason
                });
        }

        public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                new GroupChatManagerResult<bool>(false)
                {
                    Reason = "Automated workflow - no user input needed"
                });
        }

        public override ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history, CancellationToken cancellationToken = default)
        {
            var baseResult = base.ShouldTerminate(history, cancellationToken).Result;
            if (baseResult.Value)
            {
                return ValueTask.FromResult(baseResult);
            }

            var lastMessage = history.LastOrDefault();
            if (lastMessage?.AuthorName == "Critic")
            {
                string content = lastMessage.Content?.ToLower() ?? "";
                if (content.Contains("approved") || content.Contains("approve"))
                {
                    return ValueTask.FromResult(
                        new GroupChatManagerResult<bool>(true)
                        {
                            Reason = "Critic approved the slogan"
                        });
                }
            }

            return ValueTask.FromResult(
                new GroupChatManagerResult<bool>(false)
                {
                    Reason = "Continue conversation"
                });
        }
    }
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
