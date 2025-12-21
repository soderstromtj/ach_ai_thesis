using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Infrastructure.AI.Managers;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Managers;

/// <summary>
/// Unit tests for HypothesisGenerationPromptStrategy.
///
/// Testing Strategy:
/// -----------------
/// HypothesisGenerationPromptStrategy generates prompts for the hypothesis generation
/// group chat workflow. It implements IGroupChatPromptStrategy.
///
/// Key testing areas:
/// 1. GetTerminationPrompt - Termination criteria prompt generation
/// 2. GetSelectionPrompt - Agent selection prompt generation
/// 3. GetFilterPrompt - Result filtering prompt generation
/// 4. Input interpolation - Key question and agent names in prompts
/// </summary>
public class HypothesisGenerationPromptStrategyTests
{
    #region Test Infrastructure

    private static HypothesisGenerationPromptStrategy CreateStrategy()
    {
        return new HypothesisGenerationPromptStrategy();
    }

    private static OrchestrationPromptInput CreateInput(string keyQuestion = "What is the cause of the incident?")
    {
        return new OrchestrationPromptInput
        {
            KeyQuestion = keyQuestion,
            Context = "Test context",
            TaskInstructions = "Test instructions"
        };
    }

    private static List<string> CreateAgentList(params string[] names)
    {
        return names.ToList();
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// WHY: Verifies strategy can be instantiated.
    /// </summary>
    [Fact]
    public void Constructor_Always_CreatesInstance()
    {
        // Act
        var strategy = CreateStrategy();

        // Assert
        Assert.NotNull(strategy);
    }

    /// <summary>
    /// WHY: Verifies strategy implements IGroupChatPromptStrategy.
    /// </summary>
    [Fact]
    public void Strategy_ImplementsIGroupChatPromptStrategy()
    {
        // Arrange
        var strategy = CreateStrategy();

        // Assert
        Assert.IsAssignableFrom<IGroupChatPromptStrategy>(strategy);
    }

    #endregion

    #region GetTerminationPrompt - Basic Tests

    /// <summary>
    /// WHY: Verifies termination prompt is not null or empty.
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_Always_ReturnsNonEmptyString()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("Agent1", "Agent2");

        // Act
        var prompt = strategy.GetTerminationPrompt(input, agentNames);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(prompt));
    }

    /// <summary>
    /// WHY: Verifies key question is included in termination prompt.
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_IncludesKeyQuestion()
    {
        // Arrange
        var strategy = CreateStrategy();
        var keyQuestion = "Who was responsible for the security breach?";
        var input = CreateInput(keyQuestion);
        var agentNames = CreateAgentList("Agent1");

        // Act
        var prompt = strategy.GetTerminationPrompt(input, agentNames);

        // Assert
        Assert.Contains(keyQuestion, prompt);
    }

    /// <summary>
    /// WHY: Verifies all agent names are included in termination prompt.
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_IncludesAllAgentNames()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("DiplomaticAgent", "MilitaryAgent", "EconomicAgent");

        // Act
        var prompt = strategy.GetTerminationPrompt(input, agentNames);

        // Assert
        Assert.Contains("DiplomaticAgent", prompt);
        Assert.Contains("MilitaryAgent", prompt);
        Assert.Contains("EconomicAgent", prompt);
    }

    /// <summary>
    /// WHY: Verifies termination prompt mentions ACH framework.
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_MentionsACHFramework()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("Agent1");

        // Act
        var prompt = strategy.GetTerminationPrompt(input, agentNames);

        // Assert
        Assert.Contains("Analysis of Competing Hypotheses", prompt);
    }

    /// <summary>
    /// WHY: Verifies termination prompt mentions Richards Heuer.
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_MentionsRichardsHeuer()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("Agent1");

        // Act
        var prompt = strategy.GetTerminationPrompt(input, agentNames);

        // Assert
        Assert.Contains("Richards Heuer", prompt);
    }

    /// <summary>
    /// WHY: Verifies termination prompt mentions criteria for termination.
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_IncludesTerminationCriteria()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("Agent1");

        // Act
        var prompt = strategy.GetTerminationPrompt(input, agentNames);

        // Assert
        Assert.Contains("mutually exclusive", prompt);
        Assert.Contains("collectively exhaustive", prompt);
    }

    /// <summary>
    /// WHY: Verifies termination prompt expects True/False response.
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_ExpectsTrueFalseResponse()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("Agent1");

        // Act
        var prompt = strategy.GetTerminationPrompt(input, agentNames);

        // Assert
        Assert.Contains("True", prompt);
        Assert.Contains("False", prompt);
    }

    #endregion

    #region GetSelectionPrompt - Basic Tests

    /// <summary>
    /// WHY: Verifies selection prompt is not null or empty.
    /// </summary>
    [Fact]
    public void GetSelectionPrompt_Always_ReturnsNonEmptyString()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("Agent1", "Agent2");

        // Act
        var prompt = strategy.GetSelectionPrompt(input, agentNames);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(prompt));
    }

    /// <summary>
    /// WHY: Verifies key question is included in selection prompt.
    /// </summary>
    [Fact]
    public void GetSelectionPrompt_IncludesKeyQuestion()
    {
        // Arrange
        var strategy = CreateStrategy();
        var keyQuestion = "What caused the data breach?";
        var input = CreateInput(keyQuestion);
        var agentNames = CreateAgentList("Agent1");

        // Act
        var prompt = strategy.GetSelectionPrompt(input, agentNames);

        // Assert
        Assert.Contains(keyQuestion, prompt);
    }

    /// <summary>
    /// WHY: Verifies agent names are included in selection prompt.
    /// </summary>
    [Fact]
    public void GetSelectionPrompt_IncludesAgentNames()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("IntelligenceAgent", "LawEnforcementAgent");

        // Act
        var prompt = strategy.GetSelectionPrompt(input, agentNames);

        // Assert
        Assert.Contains("IntelligenceAgent", prompt);
        Assert.Contains("LawEnforcementAgent", prompt);
    }

    /// <summary>
    /// WHY: Verifies selection prompt mentions the phases.
    /// </summary>
    [Fact]
    public void GetSelectionPrompt_MentionsPhases()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("Agent1");

        // Act
        var prompt = strategy.GetSelectionPrompt(input, agentNames);

        // Assert
        Assert.Contains("Phase 1", prompt);
        Assert.Contains("Phase 2", prompt);
        Assert.Contains("Phase 3", prompt);
    }

    /// <summary>
    /// WHY: Verifies selection prompt mentions DIME-FIL agents.
    /// </summary>
    [Fact]
    public void GetSelectionPrompt_MentionsDIMEFIL()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("Agent1");

        // Act
        var prompt = strategy.GetSelectionPrompt(input, agentNames);

        // Assert
        Assert.Contains("DIME-FIL", prompt);
    }

    /// <summary>
    /// WHY: Verifies selection prompt includes example response format.
    /// </summary>
    [Fact]
    public void GetSelectionPrompt_IncludesFirstAgentAsExample()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("FirstAgent", "SecondAgent");

        // Act
        var prompt = strategy.GetSelectionPrompt(input, agentNames);

        // Assert
        Assert.Contains("FirstAgent", prompt);
    }

    #endregion

    #region GetFilterPrompt - Basic Tests

    /// <summary>
    /// WHY: Verifies filter prompt is not null or empty.
    /// </summary>
    [Fact]
    public void GetFilterPrompt_Always_ReturnsNonEmptyString()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();

        // Act
        var prompt = strategy.GetFilterPrompt(input);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(prompt));
    }

    /// <summary>
    /// WHY: Verifies key question is included in filter prompt.
    /// </summary>
    [Fact]
    public void GetFilterPrompt_IncludesKeyQuestion()
    {
        // Arrange
        var strategy = CreateStrategy();
        var keyQuestion = "What is the root cause of the failure?";
        var input = CreateInput(keyQuestion);

        // Act
        var prompt = strategy.GetFilterPrompt(input);

        // Assert
        Assert.Contains(keyQuestion, prompt);
    }

    /// <summary>
    /// WHY: Verifies filter prompt expects JSON output.
    /// </summary>
    [Fact]
    public void GetFilterPrompt_ExpectsJsonOutput()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();

        // Act
        var prompt = strategy.GetFilterPrompt(input);

        // Assert
        Assert.Contains("JSON", prompt);
    }

    /// <summary>
    /// WHY: Verifies filter prompt includes expected JSON structure.
    /// </summary>
    [Fact]
    public void GetFilterPrompt_IncludesHypothesesStructure()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();

        // Act
        var prompt = strategy.GetFilterPrompt(input);

        // Assert
        Assert.Contains("Hypotheses", prompt);
        Assert.Contains("Title", prompt);
        Assert.Contains("Rationale", prompt);
    }

    /// <summary>
    /// WHY: Verifies filter prompt mentions ACH framework.
    /// </summary>
    [Fact]
    public void GetFilterPrompt_MentionsACHFramework()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();

        // Act
        var prompt = strategy.GetFilterPrompt(input);

        // Assert
        Assert.Contains("Analysis of Competing Hypotheses", prompt);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// WHY: Verifies handling of special characters in key question.
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_WithSpecialCharactersInQuestion_HandlesGracefully()
    {
        // Arrange
        var strategy = CreateStrategy();
        var keyQuestion = "What's the \"cause\" of the <incident> & who is responsible?";
        var input = CreateInput(keyQuestion);
        var agentNames = CreateAgentList("Agent1");

        // Act
        var prompt = strategy.GetTerminationPrompt(input, agentNames);

        // Assert
        Assert.Contains(keyQuestion, prompt);
    }

    /// <summary>
    /// WHY: Verifies handling of single agent in list.
    /// </summary>
    [Fact]
    public void GetSelectionPrompt_WithSingleAgent_HandlesGracefully()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = CreateAgentList("OnlyAgent");

        // Act
        var prompt = strategy.GetSelectionPrompt(input, agentNames);

        // Assert
        Assert.Contains("OnlyAgent", prompt);
    }

    /// <summary>
    /// WHY: Verifies handling of long key question.
    /// </summary>
    [Fact]
    public void GetFilterPrompt_WithLongKeyQuestion_IncludesEntireQuestion()
    {
        // Arrange
        var strategy = CreateStrategy();
        var longQuestion = new string('x', 1000);
        var input = CreateInput(longQuestion);

        // Act
        var prompt = strategy.GetFilterPrompt(input);

        // Assert
        Assert.Contains(longQuestion, prompt);
    }

    /// <summary>
    /// WHY: Verifies handling of empty key question.
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_WithEmptyKeyQuestion_DoesNotThrow()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput("");
        var agentNames = CreateAgentList("Agent1");

        // Act
        var exception = Record.Exception(() => strategy.GetTerminationPrompt(input, agentNames));

        // Assert
        Assert.Null(exception);
    }

    /// <summary>
    /// WHY: Verifies handling of many agents.
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_WithManyAgents_IncludesAll()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput();
        var agentNames = Enumerable.Range(1, 20)
            .Select(i => $"Agent{i}")
            .ToList();

        // Act
        var prompt = strategy.GetTerminationPrompt(input, agentNames);

        // Assert
        foreach (var name in agentNames)
        {
            Assert.Contains(name, prompt);
        }
    }

    #endregion

    #region Consistency Tests

    /// <summary>
    /// WHY: Verifies same input produces same output (deterministic).
    /// </summary>
    [Fact]
    public void GetTerminationPrompt_WithSameInput_ProducesSameOutput()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input = CreateInput("Test question");
        var agentNames = CreateAgentList("Agent1", "Agent2");

        // Act
        var prompt1 = strategy.GetTerminationPrompt(input, agentNames);
        var prompt2 = strategy.GetTerminationPrompt(input, agentNames);

        // Assert
        Assert.Equal(prompt1, prompt2);
    }

    /// <summary>
    /// WHY: Verifies different questions produce different prompts.
    /// </summary>
    [Fact]
    public void GetFilterPrompt_WithDifferentQuestions_ProducesDifferentOutput()
    {
        // Arrange
        var strategy = CreateStrategy();
        var input1 = CreateInput("Question A");
        var input2 = CreateInput("Question B");

        // Act
        var prompt1 = strategy.GetFilterPrompt(input1);
        var prompt2 = strategy.GetFilterPrompt(input2);

        // Assert
        Assert.NotEqual(prompt1, prompt2);
    }

    #endregion
}
