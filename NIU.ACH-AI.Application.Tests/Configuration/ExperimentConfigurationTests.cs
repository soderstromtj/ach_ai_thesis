using FluentAssertions;
using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Application.Tests.Configuration;

public class ExperimentConfigurationTests
{
    [Fact]
    public void ToString_ReturnsFormattedStringWithDetails()
    {
        // Arrange
        var config = new ExperimentConfiguration
        {
            Id = "Exp-001",
            Name = "Test Experiment",
            Description = "A test experiment",
            KeyQuestion = "Will this pass?",
            Context = "Training context",
            ACHSteps = new ACHStepConfiguration[2] // Array of size 2
        };

        // Act
        var result = config.ToString();

        // Assert
        result.Should().Contain("Experiment ID: Exp-001");
        result.Should().Contain("Name: Test Experiment");
        result.Should().Contain("Description: A test experiment");
        result.Should().Contain("Key Question: Will this pass?");
        result.Should().Contain("Context: Training context");
        result.Should().Contain("Number of ACH Steps: 2");
    }

    [Fact]
    public void ToString_WithEmptyConfig_ReturnsFormattedStringWithDefaults()
    {
        // Arrange
        var config = new ExperimentConfiguration();

        // Act
        var result = config.ToString();

        // Assert
        result.Should().Contain("Experiment ID:");
        result.Should().Contain("Name:");
        result.Should().Contain("Number of ACH Steps: 0");
    }
}
