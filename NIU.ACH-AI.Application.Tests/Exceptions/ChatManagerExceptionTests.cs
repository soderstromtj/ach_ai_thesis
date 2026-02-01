using FluentAssertions;
using NIU.ACH_AI.Application.Exceptions;

namespace NIU.ACH_AI.Application.Tests.Exceptions;

public class ChatManagerExceptionTests
{
    [Fact]
    public void Constructor_Default_CreatesInstance()
    {
        // Act
        var exception = new ChatManagerException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeNullOrEmpty("Default exception message is usually 'Exception of type ... was thrown.'");
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Error occurred";

        // Act
        var exception = new ChatManagerException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var message = "Error occurred";
        var inner = new InvalidOperationException("Inner error");

        // Act
        var exception = new ChatManagerException(message, inner);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(inner);
    }
}
