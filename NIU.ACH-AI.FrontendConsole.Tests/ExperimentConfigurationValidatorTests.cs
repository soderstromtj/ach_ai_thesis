using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.FrontendConsole.Configuration;

namespace NIU.ACH_AI.FrontendConsole.Tests;

/// <summary>
/// Comprehensive unit tests for ExperimentConfigurationValidator.
///
/// Testing Strategy:
/// -----------------
/// ExperimentConfigurationValidator is a static utility class that validates the ExperimentConfiguration object.
/// Tests verify that valid configurations pass and invalid ones throw InvalidOperationException with specific messages.
///
/// What We Can Test:
/// 1. Validate - Ensures Id is present
/// 2. Validate - Ensures Name is present
/// 3. Validate - Ensures Description is present
/// 4. Validate - Ensures ACHSteps are present and not empty
///
/// Testing Challenges:
/// None. This is a pure function with no external dependencies.
/// </summary>
public class ExperimentConfigurationValidatorTests
{
    private static ExperimentConfiguration CreateValidConfiguration()
    {
        return new ExperimentConfiguration
        {
            Id = "EXP-001",
            Name = "Test Experiment",
            Description = "Test Description",
            ACHSteps = new[] { new ACHStepConfiguration { Id = 1 } }
        };
    }

    /// <summary>
    /// This test verifies that a fully populated, valid configuration passes validation without throwing an exception.
    /// </summary>
    [Fact]
    public void Validate_WithValidConfiguration_DoesNotThrow()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act & Assert
        try
        {
            ExperimentConfigurationValidator.Validate(config);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected no exception, but got: {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// This test verifies that validation throws an InvalidOperationException when the configuration Id is null.
    /// </summary>
    [Fact]
    public void Validate_WithNullId_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Id = null;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ExperimentConfigurationValidator.Validate(config));
        Assert.Contains("Id is not configured", exception.Message);
    }

    /// <summary>
    /// This test verifies that validation throws an InvalidOperationException when the configuration Id is empty.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyId_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Id = string.Empty;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ExperimentConfigurationValidator.Validate(config));
        Assert.Contains("Id is not configured", exception.Message);
    }

    /// <summary>
    /// This test verifies that validation throws an InvalidOperationException when the configuration Name is null.
    /// </summary>
    [Fact]
    public void Validate_WithNullName_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Name = null;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ExperimentConfigurationValidator.Validate(config));
        Assert.Contains("Name is not configured", exception.Message);
    }

    /// <summary>
    /// This test verifies that validation throws an InvalidOperationException when the configuration Name is whitespace.
    /// </summary>
    [Fact]
    public void Validate_WithWhitespaceName_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Name = "   ";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ExperimentConfigurationValidator.Validate(config));
        Assert.Contains("Name is not configured", exception.Message);
    }

    /// <summary>
    /// This test verifies that validation throws an InvalidOperationException when the configuration Description is null.
    /// </summary>
    [Fact]
    public void Validate_WithNullDescription_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.Description = null;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ExperimentConfigurationValidator.Validate(config));
        Assert.Contains("Description is not configured", exception.Message);
    }

    /// <summary>
    /// This test verifies that validation throws an InvalidOperationException when the ACHSteps array is null.
    /// </summary>
    [Fact]
    public void Validate_WithNullACHSteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.ACHSteps = null;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ExperimentConfigurationValidator.Validate(config));
        Assert.Contains("No ACH steps are configured", exception.Message);
    }

    /// <summary>
    /// This test verifies that validation throws an InvalidOperationException when the ACHSteps array is empty.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyACHSteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = CreateValidConfiguration();
        config.ACHSteps = Array.Empty<ACHStepConfiguration>();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => ExperimentConfigurationValidator.Validate(config));
        Assert.Contains("No ACH steps are configured", exception.Message);
    }
}
