using Microsoft.SemanticKernel.ChatCompletion;
using NIU.ACH_AI.Infrastructure.AI.Managers;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Managers;

/// <summary>
/// Unit tests for AgentParticipationTracker.
///
/// Testing Strategy:
/// -----------------
/// AgentParticipationTracker tracks which agents have participated in a chat conversation.
/// It analyzes ChatHistory to identify participating agents and compare against expected lists.
///
/// Key testing areas:
/// 1. HaveAllAgentsParticipated - Various participation scenarios
/// 2. GetParticipatingAgents - Agent extraction from chat history
/// 3. GetNonParticipatingAgents - Finding missing agents
/// 4. Null argument validation
/// 5. Edge cases (empty history, null author names, etc.)
/// </summary>
public class AgentParticipationTrackerTests
{
    #region Test Infrastructure

    private static AgentParticipationTracker CreateTracker()
    {
        return new AgentParticipationTracker();
    }

    private static ChatHistory CreateEmptyHistory()
    {
        return new ChatHistory();
    }

    private static ChatHistory CreateHistoryWithAgents(params string[] agentNames)
    {
        var history = new ChatHistory();
        foreach (var name in agentNames)
        {
            history.Add(new ChatMessageContent
            {
                Role = AuthorRole.Assistant,
                AuthorName = name,
                Content = $"Response from {name}"
            });
        }
        return history;
    }

    private static ChatHistory CreateHistoryWithMixedRoles(
        (AuthorRole Role, string? AuthorName)[] messages)
    {
        var history = new ChatHistory();
        foreach (var (role, authorName) in messages)
        {
            history.Add(new ChatMessageContent
            {
                Role = role,
                AuthorName = authorName,
                Content = $"Message from {authorName ?? "unknown"}"
            });
        }
        return history;
    }

    #endregion

    #region Constructor Tests

    /// <summary>
    /// WHY: Verifies tracker can be instantiated.
    /// </summary>
    [Fact]
    public void Constructor_Always_CreatesInstance()
    {
        // Act
        var tracker = CreateTracker();

        // Assert
        Assert.NotNull(tracker);
    }

    #endregion

    #region HaveAllAgentsParticipated - Basic Tests

    /// <summary>
    /// WHY: Verifies all agents participated when each has one message.
    /// </summary>
    [Fact]
    public void HaveAllAgentsParticipated_WhenAllHaveParticipated_ReturnsTrue()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1", "Agent2", "Agent3");
        var expectedAgents = new List<string> { "Agent1", "Agent2", "Agent3" };

        // Act
        var result = tracker.HaveAllAgentsParticipated(history, expectedAgents);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// WHY: Verifies not all participated when some are missing.
    /// </summary>
    [Fact]
    public void HaveAllAgentsParticipated_WhenSomeMissing_ReturnsFalse()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1", "Agent2");
        var expectedAgents = new List<string> { "Agent1", "Agent2", "Agent3" };

        // Act
        var result = tracker.HaveAllAgentsParticipated(history, expectedAgents);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// WHY: Verifies empty history means no participation.
    /// </summary>
    [Fact]
    public void HaveAllAgentsParticipated_WithEmptyHistory_ReturnsFalse()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateEmptyHistory();
        var expectedAgents = new List<string> { "Agent1" };

        // Act
        var result = tracker.HaveAllAgentsParticipated(history, expectedAgents);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// WHY: Verifies empty expected list returns true (vacuously true).
    /// </summary>
    [Fact]
    public void HaveAllAgentsParticipated_WithEmptyExpectedList_ReturnsTrue()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1");
        var expectedAgents = new List<string>();

        // Act
        var result = tracker.HaveAllAgentsParticipated(history, expectedAgents);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// WHY: Verifies more participating agents than expected still returns true.
    /// </summary>
    [Fact]
    public void HaveAllAgentsParticipated_WithMoreParticipantsThanExpected_ReturnsTrue()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1", "Agent2", "Agent3", "Agent4");
        var expectedAgents = new List<string> { "Agent1", "Agent2" };

        // Act
        var result = tracker.HaveAllAgentsParticipated(history, expectedAgents);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region HaveAllAgentsParticipated - Multiple Messages Tests

    /// <summary>
    /// WHY: Verifies agent participating multiple times is counted once.
    /// </summary>
    [Fact]
    public void HaveAllAgentsParticipated_WithDuplicateParticipation_CountsOnce()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1", "Agent1", "Agent1");
        var expectedAgents = new List<string> { "Agent1", "Agent2" };

        // Act
        var result = tracker.HaveAllAgentsParticipated(history, expectedAgents);

        // Assert
        Assert.False(result); // Agent2 still missing
    }

    #endregion

    #region HaveAllAgentsParticipated - Null Validation Tests

    /// <summary>
    /// WHY: Verifies null history throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void HaveAllAgentsParticipated_WithNullHistory_ThrowsArgumentNullException()
    {
        // Arrange
        var tracker = CreateTracker();
        var expectedAgents = new List<string> { "Agent1" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            tracker.HaveAllAgentsParticipated(null!, expectedAgents));
    }

    /// <summary>
    /// WHY: Verifies null expected agents throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void HaveAllAgentsParticipated_WithNullExpectedAgents_ThrowsArgumentNullException()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateEmptyHistory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            tracker.HaveAllAgentsParticipated(history, null!));
    }

    #endregion

    #region GetParticipatingAgents - Basic Tests

    /// <summary>
    /// WHY: Verifies participating agents are extracted correctly.
    /// </summary>
    [Fact]
    public void GetParticipatingAgents_WithMultipleAgents_ReturnsAllUniqueAgents()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1", "Agent2", "Agent3");

        // Act
        var result = tracker.GetParticipatingAgents(history);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("Agent1", result);
        Assert.Contains("Agent2", result);
        Assert.Contains("Agent3", result);
    }

    /// <summary>
    /// WHY: Verifies duplicate agents are deduplicated.
    /// </summary>
    [Fact]
    public void GetParticipatingAgents_WithDuplicates_ReturnsUniqueSet()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1", "Agent2", "Agent1", "Agent2", "Agent1");

        // Act
        var result = tracker.GetParticipatingAgents(history);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Agent1", result);
        Assert.Contains("Agent2", result);
    }

    /// <summary>
    /// WHY: Verifies empty history returns empty set.
    /// </summary>
    [Fact]
    public void GetParticipatingAgents_WithEmptyHistory_ReturnsEmptySet()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateEmptyHistory();

        // Act
        var result = tracker.GetParticipatingAgents(history);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// WHY: Verifies only Assistant role messages are considered.
    /// </summary>
    [Fact]
    public void GetParticipatingAgents_WithMixedRoles_OnlyCountsAssistantRole()
    {
        // Arrange
        var tracker = CreateTracker();
        var messages = new[]
        {
            (AuthorRole.User, "User1"),
            (AuthorRole.Assistant, "Agent1"),
            (AuthorRole.System, "System"),
            (AuthorRole.Assistant, "Agent2"),
            (AuthorRole.User, "User2")
        };
        var history = CreateHistoryWithMixedRoles(messages);

        // Act
        var result = tracker.GetParticipatingAgents(history);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Agent1", result);
        Assert.Contains("Agent2", result);
        Assert.DoesNotContain("User1", result);
        Assert.DoesNotContain("System", result);
    }

    /// <summary>
    /// WHY: Verifies null author names are filtered out.
    /// </summary>
    [Fact]
    public void GetParticipatingAgents_WithNullAuthorName_FiltersOutNull()
    {
        // Arrange
        var tracker = CreateTracker();
        var messages = new[]
        {
            (AuthorRole.Assistant, (string?)"Agent1"),
            (AuthorRole.Assistant, (string?)null),
            (AuthorRole.Assistant, (string?)"Agent2")
        };
        var history = CreateHistoryWithMixedRoles(messages);

        // Act
        var result = tracker.GetParticipatingAgents(history);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Agent1", result);
        Assert.Contains("Agent2", result);
    }

    /// <summary>
    /// WHY: Verifies empty string author names are filtered out.
    /// </summary>
    [Fact]
    public void GetParticipatingAgents_WithEmptyAuthorName_FiltersOutEmpty()
    {
        // Arrange
        var tracker = CreateTracker();
        var messages = new[]
        {
            (AuthorRole.Assistant, (string?)"Agent1"),
            (AuthorRole.Assistant, (string?)""),
            (AuthorRole.Assistant, (string?)"Agent2")
        };
        var history = CreateHistoryWithMixedRoles(messages);

        // Act
        var result = tracker.GetParticipatingAgents(history);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain("", result);
    }

    /// <summary>
    /// WHY: Verifies whitespace author names are filtered out.
    /// </summary>
    [Fact]
    public void GetParticipatingAgents_WithWhitespaceAuthorName_FiltersOutWhitespace()
    {
        // Arrange
        var tracker = CreateTracker();
        var messages = new[]
        {
            (AuthorRole.Assistant, (string?)"Agent1"),
            (AuthorRole.Assistant, (string?)"   "),
            (AuthorRole.Assistant, (string?)"Agent2")
        };
        var history = CreateHistoryWithMixedRoles(messages);

        // Act
        var result = tracker.GetParticipatingAgents(history);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region GetParticipatingAgents - Null Validation Tests

    /// <summary>
    /// WHY: Verifies null history throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetParticipatingAgents_WithNullHistory_ThrowsArgumentNullException()
    {
        // Arrange
        var tracker = CreateTracker();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            tracker.GetParticipatingAgents(null!));
    }

    #endregion

    #region GetNonParticipatingAgents - Basic Tests

    /// <summary>
    /// WHY: Verifies non-participating agents are correctly identified.
    /// </summary>
    [Fact]
    public void GetNonParticipatingAgents_WithSomeMissing_ReturnsMissingAgents()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1", "Agent3");
        var expectedAgents = new List<string> { "Agent1", "Agent2", "Agent3", "Agent4" };

        // Act
        var result = tracker.GetNonParticipatingAgents(history, expectedAgents);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("Agent2", result);
        Assert.Contains("Agent4", result);
    }

    /// <summary>
    /// WHY: Verifies empty list when all have participated.
    /// </summary>
    [Fact]
    public void GetNonParticipatingAgents_WhenAllParticipated_ReturnsEmptyList()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1", "Agent2", "Agent3");
        var expectedAgents = new List<string> { "Agent1", "Agent2", "Agent3" };

        // Act
        var result = tracker.GetNonParticipatingAgents(history, expectedAgents);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    /// WHY: Verifies all expected agents returned when none participated.
    /// </summary>
    [Fact]
    public void GetNonParticipatingAgents_WhenNoneParticipated_ReturnsAllExpected()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateEmptyHistory();
        var expectedAgents = new List<string> { "Agent1", "Agent2", "Agent3" };

        // Act
        var result = tracker.GetNonParticipatingAgents(history, expectedAgents);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("Agent1", result);
        Assert.Contains("Agent2", result);
        Assert.Contains("Agent3", result);
    }

    /// <summary>
    /// WHY: Verifies empty list when expected list is empty.
    /// </summary>
    [Fact]
    public void GetNonParticipatingAgents_WithEmptyExpectedList_ReturnsEmptyList()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1", "Agent2");
        var expectedAgents = new List<string>();

        // Act
        var result = tracker.GetNonParticipatingAgents(history, expectedAgents);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetNonParticipatingAgents - Null Validation Tests

    /// <summary>
    /// WHY: Verifies null history throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetNonParticipatingAgents_WithNullHistory_ThrowsArgumentNullException()
    {
        // Arrange
        var tracker = CreateTracker();
        var expectedAgents = new List<string> { "Agent1" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            tracker.GetNonParticipatingAgents(null!, expectedAgents));
    }

    /// <summary>
    /// WHY: Verifies null expected agents throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GetNonParticipatingAgents_WithNullExpectedAgents_ThrowsArgumentNullException()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateEmptyHistory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            tracker.GetNonParticipatingAgents(history, null!));
    }

    #endregion

    #region Case Sensitivity Tests

    /// <summary>
    /// WHY: Verifies agent name matching is case-sensitive.
    /// This is important for consistent agent identification.
    /// </summary>
    [Fact]
    public void GetParticipatingAgents_IsCaseSensitive()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1", "AGENT1", "agent1");

        // Act
        var result = tracker.GetParticipatingAgents(history);

        // Assert - All three are treated as different agents
        Assert.Equal(3, result.Count);
    }

    /// <summary>
    /// WHY: Verifies HaveAllAgentsParticipated uses case-sensitive matching.
    /// </summary>
    [Fact]
    public void HaveAllAgentsParticipated_IsCaseSensitive()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1");
        var expectedAgents = new List<string> { "AGENT1" }; // Different case

        // Act
        var result = tracker.HaveAllAgentsParticipated(history, expectedAgents);

        // Assert - Should be false because case doesn't match
        // Note: This depends on HashSet behavior which is case-sensitive by default
        Assert.False(result);
    }

    #endregion

    #region Return Type Tests

    /// <summary>
    /// WHY: Verifies GetParticipatingAgents returns HashSet for efficient lookups.
    /// </summary>
    [Fact]
    public void GetParticipatingAgents_ReturnsHashSet()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateHistoryWithAgents("Agent1");

        // Act
        var result = tracker.GetParticipatingAgents(history);

        // Assert
        Assert.IsType<HashSet<string>>(result);
    }

    /// <summary>
    /// WHY: Verifies GetNonParticipatingAgents returns List.
    /// </summary>
    [Fact]
    public void GetNonParticipatingAgents_ReturnsList()
    {
        // Arrange
        var tracker = CreateTracker();
        var history = CreateEmptyHistory();
        var expectedAgents = new List<string> { "Agent1" };

        // Act
        var result = tracker.GetNonParticipatingAgents(history, expectedAgents);

        // Assert
        Assert.IsType<List<string>>(result);
    }

    #endregion
}
