using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents a recorded response from an AI agent during a step execution.
/// Maps to the AGENT_RESPONSES table.
/// </summary>
public partial class AgentResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the agent response.
    /// </summary>
    public Guid AgentResponseId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the step execution.
    /// </summary>
    public Guid StepExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the agent configuration.
    /// </summary>
    public Guid AgentConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the name of the agent.
    /// </summary>
    public string AgentName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of input tokens used.
    /// </summary>
    public int? InputTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the number of output tokens generated.
    /// </summary>
    public int? OutputTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the length of the response content.
    /// </summary>
    public int? ContentLength { get; set; }

    /// <summary>
    /// Gets or sets the text content of the response.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the turn number in the conversation.
    /// </summary>
    public int? TurnNumber { get; set; }

    /// <summary>
    /// Gets or sets the duration of the response generation in milliseconds.
    /// </summary>
    public long? ResponseDuration { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the provider-specific completion identifier.
    /// </summary>
    public string? CompletionId { get; set; }

    /// <summary>
    /// Gets or sets the count of reasoning tokens used (for reasoning models).
    /// </summary>
    public int? ReasoningTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the count of output audio tokens.
    /// </summary>
    public int? OutputAudioTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the count of accepted prediction tokens.
    /// </summary>
    public int? AcceptedPredictionTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the count of rejected prediction tokens.
    /// </summary>
    public int? RejectedPredictionTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the count of input audio tokens.
    /// </summary>
    public int? InputAudioTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the count of cached input tokens.
    /// </summary>
    public int? CachedInputTokenCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the response generation finished.
    /// </summary>
    public DateTime FinishedAt { get; set; }

    /// <summary>
    /// Gets or sets the agent configuration associated with this response.
    /// </summary>
    public virtual AgentConfiguration AgentConfiguration { get; set; } = null!;

    /// <summary>
    /// Gets or sets the step execution associated with this response.
    /// </summary>
    public virtual StepExecution StepExecution { get; set; } = null!;
}
