using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.Configuration;

/// <summary>
/// Unit tests for ExperimentConfiguration.
///
/// Testing Strategy:
/// -----------------
/// ExperimentConfiguration represents a complete experiment with metadata
/// and ACH steps. Has a JsonPropertyName attribute for KeyIntelligenceQuestion.
///
/// Key testing areas:
/// 1. Default values
/// 2. Property assignment
/// 3. ACHSteps array handling
/// </summary>
public class ExperimentConfigurationTests
{
    #region Default Value Tests

    /// <summary>
    /// WHY: Verifies default instance has empty string properties.
    /// </summary>
    [Fact]
    public void NewInstance_HasEmptyStringDefaults()
    {
        // Arrange & Act
        var config = new ExperimentConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Id);
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.Description);
        Assert.Equal(string.Empty, config.KeyQuestion);
        Assert.Equal(string.Empty, config.Context);
    }

    /// <summary>
    /// WHY: Verifies ACHSteps defaults to empty array.
    /// </summary>
    [Fact]
    public void ACHSteps_DefaultsToEmptyArray()
    {
        // Arrange & Act
        var config = new ExperimentConfiguration();

        // Assert
        Assert.NotNull(config.ACHSteps);
        Assert.Empty(config.ACHSteps);
    }

    /// <summary>
    /// WHY: Verifies Id defaults to empty string.
    /// </summary>
    [Fact]
    public void Id_DefaultsToEmptyString()
    {
        // Arrange & Act
        var config = new ExperimentConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Id);
    }

    /// <summary>
    /// WHY: Verifies KeyQuestion defaults to empty string.
    /// </summary>
    [Fact]
    public void KeyQuestion_DefaultsToEmptyString()
    {
        // Arrange & Act
        var config = new ExperimentConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.KeyQuestion);
    }

    #endregion

    #region Property Assignment Tests

    /// <summary>
    /// WHY: Verifies all properties can be assigned.
    /// </summary>
    [Fact]
    public void AllProperties_CanBeAssigned()
    {
        // Arrange & Act
        var config = new ExperimentConfiguration
        {
            Id = "exp-001",
            Name = "Test Experiment",
            Description = "A test experiment",
            KeyQuestion = "What happened?",
            Context = "Background context"
        };

        // Assert
        Assert.Equal("exp-001", config.Id);
        Assert.Equal("Test Experiment", config.Name);
        Assert.Equal("A test experiment", config.Description);
        Assert.Equal("What happened?", config.KeyQuestion);
        Assert.Equal("Background context", config.Context);
    }

    /// <summary>
    /// WHY: Verifies ACHSteps can be assigned.
    /// </summary>
    [Fact]
    public void ACHSteps_CanBeAssigned()
    {
        // Arrange
        var steps = new[]
        {
            new ACHStepConfiguration { Id = 1, Name = "Step1" },
            new ACHStepConfiguration { Id = 2, Name = "Step2" }
        };

        // Act
        var config = new ExperimentConfiguration
        {
            ACHSteps = steps
        };

        // Assert
        Assert.Equal(2, config.ACHSteps.Length);
        Assert.Equal("Step1", config.ACHSteps[0].Name);
        Assert.Equal("Step2", config.ACHSteps[1].Name);
    }

    #endregion

    #region ACHSteps Tests

    /// <summary>
    /// WHY: Verifies single ACH step can be added.
    /// </summary>
    [Fact]
    public void ACHSteps_CanHaveSingleStep()
    {
        // Arrange & Act
        var config = new ExperimentConfiguration
        {
            ACHSteps = new[]
            {
                new ACHStepConfiguration { Id = 1, Name = "OnlyStep" }
            }
        };

        // Assert
        Assert.Single(config.ACHSteps);
    }

    /// <summary>
    /// WHY: Verifies multiple ACH steps can be added.
    /// </summary>
    [Fact]
    public void ACHSteps_CanHaveMultipleSteps()
    {
        // Arrange
        var steps = Enumerable.Range(1, 10)
            .Select(i => new ACHStepConfiguration { Id = i, Name = $"Step{i}" })
            .ToArray();

        // Act
        var config = new ExperimentConfiguration { ACHSteps = steps };

        // Assert
        Assert.Equal(10, config.ACHSteps.Length);
    }

    /// <summary>
    /// WHY: Verifies ACHSteps preserves step order.
    /// </summary>
    [Fact]
    public void ACHSteps_PreservesOrder()
    {
        // Arrange
        var config = new ExperimentConfiguration
        {
            ACHSteps = new[]
            {
                new ACHStepConfiguration { Id = 1, Name = "First" },
                new ACHStepConfiguration { Id = 2, Name = "Second" },
                new ACHStepConfiguration { Id = 3, Name = "Third" }
            }
        };

        // Assert
        Assert.Equal("First", config.ACHSteps[0].Name);
        Assert.Equal("Second", config.ACHSteps[1].Name);
        Assert.Equal("Third", config.ACHSteps[2].Name);
    }

    #endregion

    #region Full Configuration Tests

    /// <summary>
    /// WHY: Verifies complete experiment configuration.
    /// </summary>
    [Fact]
    public void FullConfiguration_CanBeCreated()
    {
        // Arrange & Act
        var config = new ExperimentConfiguration
        {
            Id = "exp-full-001",
            Name = "Full Experiment",
            Description = "Complete experiment configuration",
            KeyQuestion = "Who was responsible for the cyber attack?",
            Context = "A major corporation experienced a data breach...",
            ACHSteps = new[]
            {
                new ACHStepConfiguration
                {
                    Id = 1,
                    Name = "HypothesisBrainstorming",
                    Description = "Generate hypotheses",
                    AgentConfigurations = new[]
                    {
                        new AgentConfiguration { Name = "Agent1" }
                    }
                },
                new ACHStepConfiguration
                {
                    Id = 2,
                    Name = "EvidenceExtraction",
                    Description = "Extract evidence"
                }
            }
        };

        // Assert
        Assert.Equal("exp-full-001", config.Id);
        Assert.Equal(2, config.ACHSteps.Length);
        Assert.Single(config.ACHSteps[0].AgentConfigurations);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// WHY: Verifies handling of special characters in KeyQuestion.
    /// </summary>
    [Fact]
    public void KeyQuestion_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var config = new ExperimentConfiguration
        {
            KeyQuestion = "What's the \"root cause\" of <incident>?"
        };

        // Assert
        Assert.Contains("\"root cause\"", config.KeyQuestion);
    }

    /// <summary>
    /// WHY: Verifies handling of long context.
    /// </summary>
    [Fact]
    public void Context_CanBeLong()
    {
        // Arrange
        var longContext = new string('x', 100000);

        // Act
        var config = new ExperimentConfiguration { Context = longContext };

        // Assert
        Assert.Equal(100000, config.Context.Length);
    }

    /// <summary>
    /// WHY: Verifies ACHSteps can be replaced.
    /// </summary>
    [Fact]
    public void ACHSteps_CanBeReplaced()
    {
        // Arrange
        var config = new ExperimentConfiguration
        {
            ACHSteps = new[] { new ACHStepConfiguration { Name = "Original" } }
        };

        // Act
        config.ACHSteps = new[] { new ACHStepConfiguration { Name = "Replaced" } };

        // Assert
        Assert.Single(config.ACHSteps);
        Assert.Equal("Replaced", config.ACHSteps[0].Name);
    }

    #endregion
}
