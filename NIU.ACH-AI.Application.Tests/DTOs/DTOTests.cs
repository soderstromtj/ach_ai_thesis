using FluentAssertions;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Tests.DTOs;

public class DTOTests
{
    [Fact]
    public void TokenUsageInfo_CanSetAndGetProperties()
    {
        // Arrange
        var usage = new TokenUsageInfo
        {
            InputTokenCount = 10,
            OutputTokenCount = 20,
            ReasoningTokenCount = 5,
            OutputAudioTokenCount = 2,
            AcceptedPredictionTokenCount = 1,
            RejectedPredictionTokenCount = 0,
            InputAudioTokenCount = 3,
            CachedInputTokenCount = 4,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Assert
        usage.InputTokenCount.Should().Be(10);
        usage.OutputTokenCount.Should().Be(20);
        usage.ReasoningTokenCount.Should().Be(5);
        usage.OutputAudioTokenCount.Should().Be(2);
        usage.AcceptedPredictionTokenCount.Should().Be(1);
        usage.RejectedPredictionTokenCount.Should().Be(0);
        usage.InputAudioTokenCount.Should().Be(3);
        usage.CachedInputTokenCount.Should().Be(4);
        usage.CreatedAt.Should().NotBeNull();
    }

    [Fact]
    public void ACHWorkflowResult_CanSetAndGetProperties()
    {
        // Arrange
        var result = new ACHWorkflowResult
        {
            ExperimentId = "Exp1",
            ExperimentName = "TestExp",
            Success = true,
            ErrorMessage = "None"
        };

        // Assert
        result.ExperimentId.Should().Be("Exp1");
        result.ExperimentName.Should().Be("TestExp");
        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().Be("None");
    }

    [Fact]
    public void OrchestrationPromptInput_CanSetAndGetProperties()
    {
        // Arrange
        var input = new OrchestrationPromptInput
        {
            KeyQuestion = "What happened?",
            Context = "Some context",
            TaskInstructions = "Do this"
        };

        // Assert
        input.KeyQuestion.Should().Be("What happened?");
        input.Context.Should().Be("Some context");
        input.TaskInstructions.Should().Be("Do this");
    }

    [Fact]
    public void StepExecutionContext_CanSetAndGetProperties()
    {
        // Arrange
        var expId = Guid.NewGuid();
        var context = new StepExecutionContext
        {
            ExperimentId = expId,
            AchStepId = 1,
            AchStepName = "Step1"
        };
        context.AgentConfigurationIds.Add("Agent1", Guid.NewGuid());

        // Assert
        context.ExperimentId.Should().Be(expId);
        context.AchStepId.Should().Be(1);
        context.AchStepName.Should().Be("Step1");
        context.AgentConfigurationIds.Should().ContainKey("Agent1");
    }

    [Fact]
    public void AgentResponseRecord_CanSetAndGetProperties()
    {
        // Arrange
        var record = new AgentResponseRecord
        {
            AgentName = "Agent1",
            Content = "Hello",
            CreatedAt = DateTime.UtcNow,
            InputTokenCount = 5
        };

        // Assert
        record.AgentName.Should().Be("Agent1");
        record.Content.Should().Be("Hello");
        record.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        record.InputTokenCount.Should().Be(5);
    }
}
