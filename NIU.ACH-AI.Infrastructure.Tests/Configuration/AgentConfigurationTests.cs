using NIU.ACH_AI.Application.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.Configuration;

/// <summary>
/// Unit tests for AgentConfiguration.
///
/// Testing Strategy:
/// -----------------
/// AgentConfiguration holds agent settings including optional ServiceId and ModelId
/// for provider/model overrides.
///
/// Key testing areas:
/// 1. Default values
/// 2. Property assignment
/// 3. Optional properties (ServiceId, ModelId)
/// </summary>
public class AgentConfigurationTests
{
    #region Default Value Tests

    /// <summary>
    /// WHY: Verifies default instance has empty string properties.
    /// </summary>
    [Fact]
    public void NewInstance_HasEmptyStringDefaults()
    {
        // Arrange & Act
        var config = new AgentConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Name);
        Assert.Equal(string.Empty, config.Description);
        Assert.Equal(string.Empty, config.Instructions);
    }

    /// <summary>
    /// WHY: Verifies Name defaults to empty string.
    /// </summary>
    [Fact]
    public void Name_DefaultsToEmptyString()
    {
        // Arrange & Act
        var config = new AgentConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Name);
    }

    /// <summary>
    /// WHY: Verifies Description defaults to empty string.
    /// </summary>
    [Fact]
    public void Description_DefaultsToEmptyString()
    {
        // Arrange & Act
        var config = new AgentConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Description);
    }

    /// <summary>
    /// WHY: Verifies Instructions defaults to empty string.
    /// </summary>
    [Fact]
    public void Instructions_DefaultsToEmptyString()
    {
        // Arrange & Act
        var config = new AgentConfiguration();

        // Assert
        Assert.Equal(string.Empty, config.Instructions);
    }

    /// <summary>
    /// WHY: Verifies ServiceId defaults to null (optional).
    /// </summary>
    [Fact]
    public void ServiceId_DefaultsToNull()
    {
        // Arrange & Act
        var config = new AgentConfiguration();

        // Assert
        Assert.Null(config.ServiceId);
    }

    /// <summary>
    /// WHY: Verifies ModelId defaults to null (optional).
    /// </summary>
    [Fact]
    public void ModelId_DefaultsToNull()
    {
        // Arrange & Act
        var config = new AgentConfiguration();

        // Assert
        Assert.Null(config.ModelId);
    }

    #endregion

    #region Property Assignment Tests

    /// <summary>
    /// WHY: Verifies all required properties can be assigned.
    /// </summary>
    [Fact]
    public void RequiredProperties_CanBeAssigned()
    {
        // Arrange & Act
        var config = new AgentConfiguration
        {
            Name = "TestAgent",
            Description = "A test agent",
            Instructions = "Do something useful"
        };

        // Assert
        Assert.Equal("TestAgent", config.Name);
        Assert.Equal("A test agent", config.Description);
        Assert.Equal("Do something useful", config.Instructions);
    }

    /// <summary>
    /// WHY: Verifies ServiceId can be assigned.
    /// </summary>
    [Fact]
    public void ServiceId_CanBeAssigned()
    {
        // Arrange
        var config = new AgentConfiguration();

        // Act
        config.ServiceId = "openai";

        // Assert
        Assert.Equal("openai", config.ServiceId);
    }

    /// <summary>
    /// WHY: Verifies ModelId can be assigned.
    /// </summary>
    [Fact]
    public void ModelId_CanBeAssigned()
    {
        // Arrange
        var config = new AgentConfiguration();

        // Act
        config.ModelId = "gpt-4o";

        // Assert
        Assert.Equal("gpt-4o", config.ModelId);
    }

    /// <summary>
    /// WHY: Verifies all properties can be set in object initializer.
    /// </summary>
    [Fact]
    public void AllProperties_CanBeSetInInitializer()
    {
        // Arrange & Act
        var config = new AgentConfiguration
        {
            Name = "FullAgent",
            Description = "Full description",
            Instructions = "Full instructions",
            ServiceId = "azure",
            ModelId = "gpt-4-turbo"
        };

        // Assert
        Assert.Equal("FullAgent", config.Name);
        Assert.Equal("Full description", config.Description);
        Assert.Equal("Full instructions", config.Instructions);
        Assert.Equal("azure", config.ServiceId);
        Assert.Equal("gpt-4-turbo", config.ModelId);
    }

    #endregion

    #region ServiceId Values Tests

    /// <summary>
    /// WHY: Documents valid ServiceId values.
    /// </summary>
    [Theory]
    [InlineData("openai")]
    [InlineData("azure")]
    [InlineData("ollama")]
    public void ServiceId_AcceptsValidValues(string serviceId)
    {
        // Arrange
        var config = new AgentConfiguration();

        // Act
        config.ServiceId = serviceId;

        // Assert
        Assert.Equal(serviceId, config.ServiceId);
    }

    /// <summary>
    /// WHY: Verifies ServiceId accepts any string (no validation at property level).
    /// </summary>
    [Fact]
    public void ServiceId_AcceptsAnyString()
    {
        // Arrange
        var config = new AgentConfiguration();

        // Act
        config.ServiceId = "custom-service";

        // Assert
        Assert.Equal("custom-service", config.ServiceId);
    }

    #endregion

    #region ModelId Values Tests

    /// <summary>
    /// WHY: Documents typical ModelId values.
    /// </summary>
    [Theory]
    [InlineData("gpt-4")]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-3.5-turbo")]
    [InlineData("o1-preview")]
    [InlineData("claude-3-opus")]
    [InlineData("llama2")]
    public void ModelId_AcceptsTypicalValues(string modelId)
    {
        // Arrange
        var config = new AgentConfiguration();

        // Act
        config.ModelId = modelId;

        // Assert
        Assert.Equal(modelId, config.ModelId);
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
        var config = new AgentConfiguration
        {
            Name = "",
            Description = "",
            Instructions = "",
            ServiceId = "",
            ModelId = ""
        };

        // Assert
        Assert.Equal("", config.Name);
        Assert.Equal("", config.ServiceId);
        Assert.Equal("", config.ModelId);
    }

    /// <summary>
    /// WHY: Verifies handling of whitespace strings.
    /// </summary>
    [Fact]
    public void Properties_CanBeWhitespace()
    {
        // Arrange & Act
        var config = new AgentConfiguration
        {
            Name = "   ",
            Instructions = "\t\n"
        };

        // Assert
        Assert.Equal("   ", config.Name);
        Assert.Equal("\t\n", config.Instructions);
    }

    /// <summary>
    /// WHY: Verifies handling of long strings.
    /// </summary>
    [Fact]
    public void Instructions_CanBeLong()
    {
        // Arrange
        var longInstructions = new string('x', 50000);

        // Act
        var config = new AgentConfiguration
        {
            Instructions = longInstructions
        };

        // Assert
        Assert.Equal(50000, config.Instructions.Length);
    }

    /// <summary>
    /// WHY: Verifies handling of special characters.
    /// </summary>
    [Fact]
    public void Properties_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var config = new AgentConfiguration
        {
            Name = "Agent<Test>",
            Description = "Uses \"quotes\" and 'apostrophes'",
            Instructions = "Line1\nLine2\tTabbed"
        };

        // Assert
        Assert.Equal("Agent<Test>", config.Name);
        Assert.Contains("\"quotes\"", config.Description);
        Assert.Contains("\n", config.Instructions);
    }

    #endregion

    #region Nullability Tests

    /// <summary>
    /// WHY: Verifies ServiceId and ModelId are nullable.
    /// </summary>
    [Fact]
    public void OptionalProperties_AreNullable()
    {
        // Arrange
        var config = new AgentConfiguration
        {
            ServiceId = "test",
            ModelId = "test"
        };

        // Act - Set back to null
        config.ServiceId = null;
        config.ModelId = null;

        // Assert
        Assert.Null(config.ServiceId);
        Assert.Null(config.ModelId);
    }

    #endregion
}
