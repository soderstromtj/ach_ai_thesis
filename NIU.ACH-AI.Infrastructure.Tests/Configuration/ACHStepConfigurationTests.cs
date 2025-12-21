using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.Configuration;

/// <summary>
/// Unit tests for ACHStepConfiguration.
///
/// Testing Strategy:
/// -----------------
/// ACHStepConfiguration represents a single ACH step with agent configurations
/// and orchestration settings.
///
/// Key testing areas:
/// 1. Default values
/// 2. Property assignment
/// 3. Nested AgentConfigurations and OrchestrationSettings
/// </summary>
public class ACHStepConfigurationTests
{
    #region Default Value Tests

    /// <summary>
    /// WHY: Verifies default instance has proper defaults.
    /// </summary>
    [Fact]
    public void NewInstance_HasProperDefaults()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration();

        // Assert
        Assert.Equal(0, config.Id);
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.Description);
        Assert.Equal(string.Empty, config.TaskInstructions);
    }

    /// <summary>
    /// WHY: Verifies Id defaults to 0.
    /// </summary>
    [Fact]
    public void Id_DefaultsToZero()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration();

        // Assert
        Assert.Equal(0, config.Id);
    }

    /// <summary>
    /// WHY: Verifies AgentConfigurations defaults to empty array.
    /// </summary>
    [Fact]
    public void AgentConfigurations_DefaultsToEmptyArray()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration();

        // Assert
        Assert.NotNull(config.AgentConfigurations);
        Assert.Empty(config.AgentConfigurations);
    }

    /// <summary>
    /// WHY: Verifies OrchestrationSettings defaults to new instance.
    /// </summary>
    [Fact]
    public void OrchestrationSettings_DefaultsToNewInstance()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration();

        // Assert
        Assert.NotNull(config.OrchestrationSettings);
    }

    /// <summary>
    /// WHY: Verifies TaskInstructions defaults to empty string.
    /// </summary>
    [Fact]
    public void TaskInstructions_DefaultsToEmptyString()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.TaskInstructions);
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
        var config = new ACHStepConfiguration
        {
            Id = 1,
            Name = "HypothesisBrainstorming",
            Description = "Generate initial hypotheses",
            TaskInstructions = "Analyze the key question and propose hypotheses"
        };

        // Assert
        Assert.Equal(1, config.Id);
        Assert.Equal("HypothesisBrainstorming", config.Name);
        Assert.Equal("Generate initial hypotheses", config.Description);
        Assert.Equal("Analyze the key question and propose hypotheses", config.TaskInstructions);
    }

    /// <summary>
    /// WHY: Verifies Id can be assigned various values.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(100)]
    public void Id_CanBeAssignedVariousValues(int id)
    {
        // Arrange
        var config = new ACHStepConfiguration();

        // Act
        config.Id = id;

        // Assert
        Assert.Equal(id, config.Id);
    }

    #endregion

    #region AgentConfigurations Tests

    /// <summary>
    /// WHY: Verifies AgentConfigurations can be assigned.
    /// </summary>
    [Fact]
    public void AgentConfigurations_CanBeAssigned()
    {
        // Arrange
        var agents = new[]
        {
            new AgentConfiguration { Name = "Agent1" },
            new AgentConfiguration { Name = "Agent2" }
        };

        // Act
        var config = new ACHStepConfiguration
        {
            AgentConfigurations = agents
        };

        // Assert
        Assert.Equal(2, config.AgentConfigurations.Length);
    }

    /// <summary>
    /// WHY: Verifies single agent can be configured.
    /// </summary>
    [Fact]
    public void AgentConfigurations_CanHaveSingleAgent()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration
        {
            AgentConfigurations = new[]
            {
                new AgentConfiguration { Name = "OnlyAgent" }
            }
        };

        // Assert
        Assert.Single(config.AgentConfigurations);
        Assert.Equal("OnlyAgent", config.AgentConfigurations[0].Name);
    }

    /// <summary>
    /// WHY: Verifies many agents can be configured.
    /// </summary>
    [Fact]
    public void AgentConfigurations_CanHaveManyAgents()
    {
        // Arrange
        var agents = Enumerable.Range(1, 20)
            .Select(i => new AgentConfiguration { Name = $"Agent{i}" })
            .ToArray();

        // Act
        var config = new ACHStepConfiguration
        {
            AgentConfigurations = agents
        };

        // Assert
        Assert.Equal(20, config.AgentConfigurations.Length);
    }

    /// <summary>
    /// WHY: Verifies agent configurations preserve all properties.
    /// </summary>
    [Fact]
    public void AgentConfigurations_PreservesAgentProperties()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration
        {
            AgentConfigurations = new[]
            {
                new AgentConfiguration
                {
                    Name = "TestAgent",
                    Description = "Test Description",
                    Instructions = "Test Instructions",
                    ServiceId = "openai",
                    ModelId = "gpt-4o"
                }
            }
        };

        // Assert
        var agent = config.AgentConfigurations[0];
        Assert.Equal("TestAgent", agent.Name);
        Assert.Equal("Test Description", agent.Description);
        Assert.Equal("Test Instructions", agent.Instructions);
        Assert.Equal("openai", agent.ServiceId);
        Assert.Equal("gpt-4o", agent.ModelId);
    }

    #endregion

    #region OrchestrationSettings Tests

    /// <summary>
    /// WHY: Verifies OrchestrationSettings can be assigned.
    /// </summary>
    [Fact]
    public void OrchestrationSettings_CanBeAssigned()
    {
        // Arrange
        var settings = new OrchestrationSettings
        {
            MaximumInvocationCount = 20,
            TimeoutInMinutes = 30
        };

        // Act
        var config = new ACHStepConfiguration
        {
            OrchestrationSettings = settings
        };

        // Assert
        Assert.Equal(20, config.OrchestrationSettings.MaximumInvocationCount);
        Assert.Equal(30, config.OrchestrationSettings.TimeoutInMinutes);
    }

    /// <summary>
    /// WHY: Verifies default OrchestrationSettings values.
    /// </summary>
    [Fact]
    public void OrchestrationSettings_HasDefaultValues()
    {
        // Arrange
        var config = new ACHStepConfiguration();

        // Assert - Check OrchestrationSettings exists with defaults
        Assert.NotNull(config.OrchestrationSettings);
    }

    #endregion

    #region Full Configuration Tests

    /// <summary>
    /// WHY: Verifies complete step configuration.
    /// </summary>
    [Fact]
    public void FullConfiguration_CanBeCreated()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration
        {
            Id = 1,
            Name = "HypothesisBrainstorming",
            Description = "Generate hypotheses for the key question",
            TaskInstructions = "Analyze the scenario and propose competing hypotheses",
            AgentConfigurations = new[]
            {
                new AgentConfiguration
                {
                    Name = "DiplomaticAgent",
                    Description = "Focuses on diplomatic hypotheses",
                    Instructions = "Consider diplomatic angles"
                },
                new AgentConfiguration
                {
                    Name = "MilitaryAgent",
                    Description = "Focuses on military hypotheses",
                    Instructions = "Consider military angles"
                }
            },
            OrchestrationSettings = new OrchestrationSettings
            {
                MaximumInvocationCount = 15,
                TimeoutInMinutes = 20,
                StreamResponses = true,
                WriteResponses = false
            }
        };

        // Assert
        Assert.Equal(1, config.Id);
        Assert.Equal("HypothesisBrainstorming", config.Name);
        Assert.Equal(2, config.AgentConfigurations.Length);
        Assert.Equal(15, config.OrchestrationSettings.MaximumInvocationCount);
        Assert.True(config.OrchestrationSettings.StreamResponses);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// WHY: Verifies handling of empty string properties.
    /// </summary>
    [Fact]
    public void Properties_CanBeEmptyStrings()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration
        {
            Name = "",
            Description = "",
            TaskInstructions = ""
        };

        // Assert
        Assert.Equal("", config.Name);
        Assert.Equal("", config.Description);
        Assert.Equal("", config.TaskInstructions);
    }

    /// <summary>
    /// WHY: Verifies handling of long TaskInstructions.
    /// </summary>
    [Fact]
    public void TaskInstructions_CanBeLong()
    {
        // Arrange
        var longInstructions = new string('x', 50000);

        // Act
        var config = new ACHStepConfiguration
        {
            TaskInstructions = longInstructions
        };

        // Assert
        Assert.Equal(50000, config.TaskInstructions.Length);
    }

    /// <summary>
    /// WHY: Verifies AgentConfigurations can be replaced.
    /// </summary>
    [Fact]
    public void AgentConfigurations_CanBeReplaced()
    {
        // Arrange
        var config = new ACHStepConfiguration
        {
            AgentConfigurations = new[] { new AgentConfiguration { Name = "Original" } }
        };

        // Act
        config.AgentConfigurations = new[] { new AgentConfiguration { Name = "Replaced" } };

        // Assert
        Assert.Single(config.AgentConfigurations);
        Assert.Equal("Replaced", config.AgentConfigurations[0].Name);
    }

    /// <summary>
    /// WHY: Verifies negative Id is allowed (no validation).
    /// </summary>
    [Fact]
    public void Id_CanBeNegative()
    {
        // Arrange & Act
        var config = new ACHStepConfiguration { Id = -1 };

        // Assert
        Assert.Equal(-1, config.Id);
    }

    #endregion
}
