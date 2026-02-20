using FluentAssertions;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Services;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Tests.Services;

public class OrchestrationPromptFormatterTests
{
    private readonly OrchestrationPromptFormatter _formatter;

    public OrchestrationPromptFormatterTests()
    {
        _formatter = new OrchestrationPromptFormatter();
    }

    [Fact]
    public void FormatPrompt_WithEmptyInput_ReturnsEmptyString()
    {
        var result = _formatter.FormatPrompt(null!);
        result.Should().BeEmpty();
    }

    [Fact]
    public void FormatPrompt_IncludesAllPopulatedFields()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            KeyQuestion = "Test KQ",
            Context = "Test Context",
            TaskInstructions = "Test Instructions",
            AdditionalInstructions = "Test Additional"
        };

        // Act
        var result = _formatter.FormatPrompt(input);

        // Assert
        result.Should().Contain("Key Question: Test KQ");
        result.Should().Contain("Context: Test Context");
        result.Should().Contain("Task Instructions: Test Instructions");
        result.Should().Contain("Additional Instructions: Test Additional");
        result.Should().NotContain("Hypotheses:");
        result.Should().NotContain("Evidence:");
    }

    [Fact]
    public void FormatPrompt_IncludesSerializedResults_WhenProvided()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            KeyQuestion = "Q",
            Context = "C",
            TaskInstructions = "I",
            HypothesisResult = new HypothesisResult { Hypotheses = new List<Hypothesis> { new Hypothesis { HypothesisText = "H1" } } }
        };

        // Act
        var result = _formatter.FormatPrompt(input);

        // Assert
        result.Should().Contain("Hypotheses: ");
        result.Should().Contain("H1");
        result.Should().NotContain("Evidence:");
    }
}
