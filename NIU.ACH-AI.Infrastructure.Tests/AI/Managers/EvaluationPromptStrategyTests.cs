using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Infrastructure.AI.Managers;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Managers;

/// <summary>
/// Unit tests for EvaluationPromptStrategy.
///
/// Testing Strategy:
/// -----------------
/// EvaluationPromptStrategy consists of pure functions that generate prompt strings.
/// Tests verify that the generated strings contain key expected elements (agent names, protocol instructions).
/// </summary>
public class EvaluationPromptStrategyTests
{
    private readonly EvaluationPromptStrategy _strategy;
    private readonly OrchestrationPromptInput _input;
    private readonly List<string> _agentNames;

    public EvaluationPromptStrategyTests()
    {
        _strategy = new EvaluationPromptStrategy();
        _input = new OrchestrationPromptInput(); // Input content rarely affects the template structure in these specific methods
        _agentNames = new List<string> { "AgentA", "AgentB" };
    }

    [Fact]
    public void GetTerminationPrompt_ContainsAgentNames()
    {
        // Act
        var prompt = _strategy.GetTerminationPrompt(_input, _agentNames);

        // Assert
        Assert.NotNull(prompt);
        Assert.Contains("AgentA", prompt);
        Assert.Contains("AgentB", prompt);
    }

    [Fact]
    public void GetTerminationPrompt_ContainsProtocolInstructions()
    {
        // Act
        var prompt = _strategy.GetTerminationPrompt(_input, _agentNames);

        // Assert
        Assert.Contains("True", prompt); // Should mention True/False instructions
        Assert.Contains("False", prompt);
        Assert.Contains("Reviewer", prompt);
        Assert.Contains("Summarizer", prompt);
    }

    [Fact]
    public void GetSelectionPrompt_ContainsAgentNamesAndExample()
    {
        // Act
        var prompt = _strategy.GetSelectionPrompt(_input, _agentNames);

        // Assert
        Assert.NotNull(prompt);
        // Ensure it uses the first agent name in the example
        Assert.Contains(_agentNames.First(), prompt);
    }

    [Fact]
    public void GetSelectionPrompt_ContainsTransitionRules()
    {
        // Act
        var prompt = _strategy.GetSelectionPrompt(_input, _agentNames);

        // Assert
        Assert.Contains("CHECK LAST SPEAKER", prompt);
        Assert.Contains("CHECK DIME COMPLETION", prompt);
        Assert.Contains("ReviewerAgent", prompt);
        Assert.Contains("SummarizerAgent", prompt);
    }

    [Fact]
    public void GetFilterPrompt_ContainsJsonStructure()
    {
        // Act
        var prompt = _strategy.GetFilterPrompt(_input);

        // Assert
        Assert.NotNull(prompt);
        Assert.Contains("JSON", prompt);
        Assert.Contains("Score", prompt);
        Assert.Contains("ScoreRationale", prompt);
        Assert.Contains("ConfidenceLevel", prompt);
        Assert.Contains("ConfidenceRationale", prompt);
    }
}
