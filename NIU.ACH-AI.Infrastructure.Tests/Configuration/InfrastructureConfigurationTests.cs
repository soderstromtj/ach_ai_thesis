using FluentAssertions;
using NIU.ACH_AI.Infrastructure.Configuration;

namespace NIU.ACH_AI.Infrastructure.Tests.Configuration;

public class InfrastructureConfigurationTests
{
    [Fact]
    public void AIServiceSettings_HasCorrectDefaults()
    {
        // Arrange & Act
        var settings = new AIServiceSettings();

        // Assert
        settings.HttpTimeoutSeconds.Should().Be(300);
        settings.AzureOpenAI.Should().BeNull();
        settings.OpenAI.Should().BeNull();
        settings.Ollama.Should().BeNull();
    }

    [Fact]
    public void AzureOpenAISettings_HasCorrectDefaults()
    {
        // Arrange & Act
        var settings = new AzureOpenAISettings();

        // Assert
        settings.ApiKey.Should().BeEmpty();
        settings.Endpoint.Should().BeEmpty();
        settings.DeploymentName.Should().BeEmpty();
        settings.ModelId.Should().BeNull();
        settings.ServiceId.Should().BeNull();
    }

    [Fact]
    public void OllamaSettings_HasCorrectDefaults()
    {
        // Arrange & Act
        var settings = new OllamaSettings();

        // Assert
        settings.Endpoint.Should().Be("http://localhost:11434");
        settings.ModelId.Should().Be("llama2");
        settings.ServiceId.Should().BeNull();
    }

    [Fact]
    public void OpenAISettings_HasCorrectDefaults()
    {
        // Arrange & Act
        var settings = new OpenAISettings();

        // Assert
        settings.ApiKey.Should().BeEmpty();
        settings.ModelId.Should().Be("o3");
        settings.OrganizationId.Should().BeNull();
        settings.ServiceId.Should().BeNull();
    }
}
