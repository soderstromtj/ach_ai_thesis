using FluentAssertions;
using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Application.Tests.Configuration;

public class ConfigurationTests
{
    [Fact]
    public void ACHStepConfiguration_HasCorrectDefaults()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration();

        // Assert
        config.Name.Should().BeEmpty();
        config.Description.Should().BeEmpty();
        config.TaskInstructions.Should().BeEmpty();
        config.AgentConfigurations.Should().BeEmpty();
        config.OrchestrationSettings.Should().NotBeNull();
    }

    [Fact]
    public void AgentConfiguration_HasCorrectDefaults()
    {
        // Arrange & Act
        var config = new AgentConfiguration();

        // Assert
        config.Name.Should().BeEmpty();
        config.Description.Should().BeEmpty();
        config.Instructions.Should().BeEmpty();
        config.ServiceId.Should().BeNull();
        config.ModelId.Should().BeNull();
        config.Tags.Should().BeEmpty();
    }

    [Fact]
    public void OrchestrationSettings_HasCorrectDefaults()
    {
        // Arrange & Act
        var settings = new OrchestrationSettings();

        // Assert
        settings.MaximumInvocationCount.Should().Be(10);
        settings.TimeoutInMinutes.Should().Be(15);
        settings.StreamResponses.Should().BeFalse();
        settings.WriteResponses.Should().BeTrue();
    }

    [Fact]
    public void ExperimentsSettings_HasCorrectDefaults()
    {
        // Arrange & Act
        var settings = new ExperimentsSettings();

        // Assert
        settings.Experiments.Should().BeEmpty();
    }
}
