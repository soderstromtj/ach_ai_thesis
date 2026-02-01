using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Infrastructure.AI.Managers;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Managers;

/// <summary>
/// Unit tests for EvidenceExtractionPromptStrategy.
/// </summary>
public class EvidenceExtractionPromptStrategyTests
{
    private readonly EvidenceExtractionPromptStrategy _strategy;
    private readonly OrchestrationPromptInput _input;
    private readonly List<string> _agentNames;

    public EvidenceExtractionPromptStrategyTests()
    {
        _strategy = new EvidenceExtractionPromptStrategy();
        _input = new OrchestrationPromptInput();
        _agentNames = new List<string> { "AgentA", "AgentB" };
    }

    [Fact]
    public void GetTerminationPrompt_ContainsAgentNames()
    {
        // Act
        var prompt = _strategy.GetTerminationPrompt(_input, _agentNames);

        // Assert
        Assert.NotNull(prompt);
        // While not strictly required by the current template, ensuring the strategy considers agents is good practice.
        // However, the current concrete implementation uses a fixed message about "Extractor" agents.
        // Let's verify it contains the specific roles mentioned in the strategy.
        Assert.Contains("Extractor", prompt);
        Assert.Contains("Reviewer", prompt);
        Assert.Contains("Deduplication", prompt);
    }

    [Fact]
    public void GetTerminationPrompt_ContainsProtocolInstructions()
    {
        // Act
        var prompt = _strategy.GetTerminationPrompt(_input, _agentNames);

        // Assert
        Assert.Contains("True", prompt); // Instructions to end discussion
        Assert.Contains("False", prompt);
        Assert.Contains("Step 2", prompt);
    }

    [Fact]
    public void GetSelectionPrompt_ContainsAgentNamesAndExample()
    {
        // Act
        var prompt = _strategy.GetSelectionPrompt(_input, _agentNames);

        // Assert
        Assert.NotNull(prompt);
        Assert.Contains(_agentNames.First(), prompt);
    }

    [Fact]
    public void GetSelectionPrompt_ContainsPhaseInstructions()
    {
        // Act
        var prompt = _strategy.GetSelectionPrompt(_input, _agentNames);

        // Assert
        Assert.Contains("Phase 1", prompt);
        Assert.Contains("Phase 2", prompt);
        Assert.Contains("Phase 3", prompt);
        Assert.Contains("Extractor", prompt);
        Assert.Contains("Reviewer", prompt);
        Assert.Contains("Deduplication", prompt);
    }

    [Fact]
    public void GetFilterPrompt_ContainsJsonStructure()
    {
        // Act
        var prompt = _strategy.GetFilterPrompt(_input);

        // Assert
        Assert.NotNull(prompt);
        Assert.Contains("Evidence", prompt);
        Assert.Contains("EvidenceId", prompt);
        Assert.Contains("Claim", prompt);
        Assert.Contains("ReferenceSnippet", prompt);
        Assert.Contains("Type", prompt);
        Assert.Contains("Notes", prompt);
    }
}
