using System.Text;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Domain.ValueObjects;
using NIU.ACH_AI.FrontendConsole.Presentation;

namespace NIU.ACH_AI.FrontendConsole.Tests;

/// <summary>
/// Unit tests for ConsoleResultPresenter.
///
/// Testing Strategy:
/// -----------------
/// ConsoleResultPresenter is a CONCRETE class that implements IResultPresenter.
/// It is responsible for formatting and displaying ACH analysis results to the console.
/// The class encapsulates all presentation logic, providing a clean separation between
/// business logic and output formatting.
///
/// Testing Approach:
/// Since ConsoleResultPresenter directly uses Console.WriteLine, we redirect Console.Out
/// to a StringWriter to capture output for verification. This allows us to:
/// - Verify exact output formatting
/// - Ensure proper separator usage
/// - Validate that all required information is displayed
/// - Test without actual console I/O (keeping tests fast and isolated)
///
/// What We Test:
/// 1. DisplayExperimentInfo - Displays experiment configuration with separators
/// 2. DisplayHypotheses - Formats and displays hypothesis collections with title
/// 3. DisplayEvidence - Formats and displays evidence collections with title
/// 4. DisplayEvaluation - Displays evaluation results
/// 5. DisplayErrorMessage - Displays error messages with ERROR prefix
/// 6. Argument Validation - All methods properly validate null/empty arguments
/// 7. Edge Cases - Empty collections, special characters, whitespace, boundary values
/// 8. Interface Implementation - Verify IResultPresenter contract is fulfilled
///
/// Testing Principles:
/// - FIRST: Fast (no real I/O), Isolated (output capture), Repeatable, Self-validating, Timely
/// - Arrange-Act-Assert structure in all tests
/// - One behavior per test for clear failure diagnostics
/// - Comprehensive edge case coverage
/// - Descriptive test names documenting expected behavior
/// </summary>
public class ConsoleResultPresenterTests : IDisposable
{
    private readonly ConsoleResultPresenter _presenter;
    private StringWriter _stringWriter;
    private TextWriter _originalOutput;

    public ConsoleResultPresenterTests()
    {
        _presenter = new ConsoleResultPresenter();
        _originalOutput = Console.Out;
        _stringWriter = new StringWriter();
        Console.SetOut(_stringWriter);
    }

    public void Dispose()
    {
        Console.SetOut(_originalOutput);
        _stringWriter.Dispose();
    }

    private string GetOutput()
    {
        return _stringWriter.ToString();
    }

    private void ClearOutput()
    {
        _stringWriter.GetStringBuilder().Clear();
    }

    #region Test Infrastructure

    /// <summary>
    /// Creates a sample experiment configuration for testing.
    /// </summary>
    private static ExperimentConfiguration CreateSampleExperimentConfiguration()
    {
        return new ExperimentConfiguration
        {
            Id = "EXP-001",
            Name = "Test Experiment",
            Description = "Test Description",
            KeyQuestion = "What is the test question?",
            Context = "Test context information",
            ACHSteps = new[]
            {
                new ACHStepConfiguration
                {
                    StepName = "Brainstorming",
                    AgentConfigurations = Array.Empty<AgentConfiguration>(),
                    OrchestrationSettings = new OrchestrationSettings()
                }
            }
        };
    }

    /// <summary>
    /// Creates sample hypotheses for testing.
    /// </summary>
    private static List<Hypothesis> CreateSampleHypotheses(int count)
    {
        var hypotheses = new List<Hypothesis>();
        for (int i = 0; i < count; i++)
        {
            hypotheses.Add(new Hypothesis
            {
                HypothesisId = Guid.NewGuid(),
                ShortTitle = $"Hypothesis {i + 1}",
                HypothesisText = $"Full text of hypothesis {i + 1}",
                IsRefined = false
            });
        }
        return hypotheses;
    }

    /// <summary>
    /// Creates sample evidence for testing.
    /// </summary>
    private static List<Evidence> CreateSampleEvidence(int count)
    {
        var evidence = new List<Evidence>();
        for (int i = 0; i < count; i++)
        {
            evidence.Add(new Evidence
            {
                EvidenceId = Guid.NewGuid(),
                Claim = $"Evidence claim {i + 1}",
                ReferenceSnippet = $"Reference {i + 1}",
                Type = i % 2 == 0 ? EvidenceType.Fact : EvidenceType.Assumption,
                Notes = $"Notes {i + 1}"
            });
        }
        return evidence;
    }

    /// <summary>
    /// Creates a sample evaluation for testing.
    /// </summary>
    private static EvidenceHypothesisEvaluation CreateSampleEvaluation()
    {
        return new EvidenceHypothesisEvaluation
        {
            EvidenceId = Guid.NewGuid(),
            HypothesisId = Guid.NewGuid(),
            Consistency = Consistency.Consistent,
            Relevance = Relevance.VeryRelevant,
            Reasoning = "Test reasoning"
        };
    }

    #endregion

    #region DisplayExperimentInfo Tests

    /// <summary>
    /// Verifies that DisplayExperimentInfo outputs the experiment configuration and separator.
    /// </summary>
    [Fact]
    public void DisplayExperimentInfo_WithValidConfiguration_DisplaysConfigurationAndSeparator()
    {
        // Arrange
        var config = CreateSampleExperimentConfiguration();

        // Act
        _presenter.DisplayExperimentInfo(config);
        var output = GetOutput();

        // Assert
        Assert.Contains(config.ToString(), output);
        Assert.Contains(new string('=', 70), output);
    }

    /// <summary>
    /// Verifies that DisplayExperimentInfo throws ArgumentNullException when configuration is null.
    /// </summary>
    [Fact]
    public void DisplayExperimentInfo_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        ExperimentConfiguration? nullConfig = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _presenter.DisplayExperimentInfo(nullConfig!));
    }

    /// <summary>
    /// Verifies that DisplayExperimentInfo throws ArgumentException when configuration name is null.
    /// </summary>
    [Fact]
    public void DisplayExperimentInfo_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateSampleExperimentConfiguration();
        config.Name = null!;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayExperimentInfo(config));
    }

    /// <summary>
    /// Verifies that DisplayExperimentInfo throws ArgumentException when configuration name is empty.
    /// </summary>
    [Fact]
    public void DisplayExperimentInfo_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateSampleExperimentConfiguration();
        config.Name = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayExperimentInfo(config));
    }

    /// <summary>
    /// Verifies that DisplayExperimentInfo throws ArgumentException when KeyQuestion is null.
    /// </summary>
    [Fact]
    public void DisplayExperimentInfo_WithNullKeyQuestion_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateSampleExperimentConfiguration();
        config.KeyQuestion = null!;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayExperimentInfo(config));
    }

    /// <summary>
    /// Verifies that DisplayExperimentInfo throws ArgumentException when KeyQuestion is empty.
    /// </summary>
    [Fact]
    public void DisplayExperimentInfo_WithEmptyKeyQuestion_ThrowsArgumentException()
    {
        // Arrange
        var config = CreateSampleExperimentConfiguration();
        config.KeyQuestion = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayExperimentInfo(config));
    }

    /// <summary>
    /// Verifies that DisplayExperimentInfo handles configuration with special characters.
    /// </summary>
    [Fact]
    public void DisplayExperimentInfo_WithSpecialCharacters_DisplaysCorrectly()
    {
        // Arrange
        var config = CreateSampleExperimentConfiguration();
        config.Name = "Test <>&\" Special";
        config.KeyQuestion = "What about émojis: 🎉?";

        // Act
        _presenter.DisplayExperimentInfo(config);
        var output = GetOutput();

        // Assert
        Assert.Contains(config.Name, output);
        Assert.Contains(config.KeyQuestion, output);
    }

    #endregion

    #region DisplayHypotheses Tests

    /// <summary>
    /// Verifies that DisplayHypotheses displays title, separator, and all hypotheses.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithValidInput_DisplaysTitleSeparatorAndHypotheses()
    {
        // Arrange
        var title = "Test Hypotheses";
        var hypotheses = CreateSampleHypotheses(2);

        // Act
        _presenter.DisplayHypotheses(title, hypotheses);
        var output = GetOutput();

        // Assert
        Assert.Contains(title, output);
        Assert.Contains(new string('=', 70), output);
        foreach (var hypothesis in hypotheses)
        {
            Assert.Contains(hypothesis.ToString(), output);
        }
    }

    /// <summary>
    /// Verifies that DisplayHypotheses handles empty collection without errors.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithEmptyCollection_DisplaysTitleAndSeparatorOnly()
    {
        // Arrange
        var title = "Empty Hypotheses";
        var hypotheses = new List<Hypothesis>();

        // Act
        _presenter.DisplayHypotheses(title, hypotheses);
        var output = GetOutput();

        // Assert
        Assert.Contains(title, output);
        Assert.Contains(new string('=', 70), output);
    }

    /// <summary>
    /// Verifies that DisplayHypotheses throws ArgumentNullException when hypotheses collection is null.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var title = "Test";
        IEnumerable<Hypothesis>? nullHypotheses = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _presenter.DisplayHypotheses(title, nullHypotheses!));
    }

    /// <summary>
    /// Verifies that DisplayHypotheses throws ArgumentException when title is null.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithNullTitle_ThrowsArgumentException()
    {
        // Arrange
        string? nullTitle = null;
        var hypotheses = CreateSampleHypotheses(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayHypotheses(nullTitle!, hypotheses));
    }

    /// <summary>
    /// Verifies that DisplayHypotheses throws ArgumentException when title is empty.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithEmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var emptyTitle = string.Empty;
        var hypotheses = CreateSampleHypotheses(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayHypotheses(emptyTitle, hypotheses));
    }

    /// <summary>
    /// Verifies that DisplayHypotheses handles single hypothesis correctly.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithSingleHypothesis_DisplaysCorrectly()
    {
        // Arrange
        var title = "Single Hypothesis";
        var hypotheses = CreateSampleHypotheses(1);

        // Act
        _presenter.DisplayHypotheses(title, hypotheses);
        var output = GetOutput();

        // Assert
        Assert.Contains(title, output);
        Assert.Contains(hypotheses[0].ToString(), output);
    }

    /// <summary>
    /// Verifies that DisplayHypotheses handles large collections without errors.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithLargeCollection_HandlesCorrectly()
    {
        // Arrange
        var title = "Many Hypotheses";
        var hypotheses = CreateSampleHypotheses(100);

        // Act
        _presenter.DisplayHypotheses(title, hypotheses);
        var output = GetOutput();

        // Assert
        Assert.Contains(title, output);
        // Verify all hypotheses are displayed
        foreach (var hypothesis in hypotheses)
        {
            Assert.Contains(hypothesis.ToString(), output);
        }
    }

    /// <summary>
    /// Verifies that DisplayHypotheses handles special characters in title.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithSpecialCharactersInTitle_DisplaysCorrectly()
    {
        // Arrange
        var title = "Hypotheses: <Test & \"Special\">";
        var hypotheses = CreateSampleHypotheses(1);

        // Act
        _presenter.DisplayHypotheses(title, hypotheses);
        var output = GetOutput();

        // Assert
        Assert.Contains(title, output);
    }

    /// <summary>
    /// Verifies that DisplayHypotheses correctly outputs separator before title.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_Always_DisplaysSeparatorBeforeTitle()
    {
        // Arrange
        var title = "Test";
        var hypotheses = CreateSampleHypotheses(1);

        // Act
        _presenter.DisplayHypotheses(title, hypotheses);
        var output = GetOutput();

        // Assert
        var separatorIndex = output.IndexOf(new string('=', 70));
        var titleIndex = output.IndexOf(title);
        Assert.True(separatorIndex < titleIndex, "Separator should appear before title");
    }

    #endregion

    #region DisplayEvidence Tests

    /// <summary>
    /// Verifies that DisplayEvidence displays title, separator, and all evidence items.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithValidInput_DisplaysTitleSeparatorAndEvidence()
    {
        // Arrange
        var title = "Test Evidence";
        var evidence = CreateSampleEvidence(2);

        // Act
        _presenter.DisplayEvidence(title, evidence);
        var output = GetOutput();

        // Assert
        Assert.Contains(title, output);
        Assert.Contains(new string('=', 70), output);
        foreach (var ev in evidence)
        {
            Assert.Contains(ev.ToString(), output);
        }
    }

    /// <summary>
    /// Verifies that DisplayEvidence handles empty collection without errors.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithEmptyCollection_DisplaysTitleAndSeparatorOnly()
    {
        // Arrange
        var title = "Empty Evidence";
        var evidence = new List<Evidence>();

        // Act
        _presenter.DisplayEvidence(title, evidence);
        var output = GetOutput();

        // Assert
        Assert.Contains(title, output);
        Assert.Contains(new string('=', 70), output);
    }

    /// <summary>
    /// Verifies that DisplayEvidence throws ArgumentNullException when evidence collection is null.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var title = "Test";
        IEnumerable<Evidence>? nullEvidence = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _presenter.DisplayEvidence(title, nullEvidence!));
    }

    /// <summary>
    /// Verifies that DisplayEvidence throws ArgumentException when title is null.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithNullTitle_ThrowsArgumentException()
    {
        // Arrange
        string? nullTitle = null;
        var evidence = CreateSampleEvidence(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayEvidence(nullTitle!, evidence));
    }

    /// <summary>
    /// Verifies that DisplayEvidence throws ArgumentException when title is empty.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithEmptyTitle_ThrowsArgumentException()
    {
        // Arrange
        var emptyTitle = string.Empty;
        var evidence = CreateSampleEvidence(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayEvidence(emptyTitle, evidence));
    }

    /// <summary>
    /// Verifies that DisplayEvidence handles single evidence item correctly.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithSingleItem_DisplaysCorrectly()
    {
        // Arrange
        var title = "Single Evidence";
        var evidence = CreateSampleEvidence(1);

        // Act
        _presenter.DisplayEvidence(title, evidence);
        var output = GetOutput();

        // Assert
        Assert.Contains(title, output);
        Assert.Contains(evidence[0].ToString(), output);
    }

    /// <summary>
    /// Verifies that DisplayEvidence handles large collections without errors.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithLargeCollection_HandlesCorrectly()
    {
        // Arrange
        var title = "Many Evidence Items";
        var evidence = CreateSampleEvidence(50);

        // Act
        _presenter.DisplayEvidence(title, evidence);
        var output = GetOutput();

        // Assert
        Assert.Contains(title, output);
        foreach (var ev in evidence)
        {
            Assert.Contains(ev.ToString(), output);
        }
    }

    /// <summary>
    /// Verifies that DisplayEvidence handles both Fact and Assumption evidence types.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithMixedEvidenceTypes_DisplaysBothTypes()
    {
        // Arrange
        var title = "Mixed Evidence";
        var evidence = new List<Evidence>
        {
            new Evidence { EvidenceId = Guid.NewGuid(), Claim = "Fact evidence", Type = EvidenceType.Fact },
            new Evidence { EvidenceId = Guid.NewGuid(), Claim = "Assumption evidence", Type = EvidenceType.Assumption }
        };

        // Act
        _presenter.DisplayEvidence(title, evidence);
        var output = GetOutput();

        // Assert
        Assert.Contains(evidence[0].ToString(), output);
        Assert.Contains(evidence[1].ToString(), output);
    }

    /// <summary>
    /// Verifies that DisplayEvidence correctly outputs separator before title.
    /// </summary>
    [Fact]
    public void DisplayEvidence_Always_DisplaysSeparatorBeforeTitle()
    {
        // Arrange
        var title = "Test";
        var evidence = CreateSampleEvidence(1);

        // Act
        _presenter.DisplayEvidence(title, evidence);
        var output = GetOutput();

        // Assert
        var separatorIndex = output.IndexOf(new string('=', 70));
        var titleIndex = output.IndexOf(title);
        Assert.True(separatorIndex < titleIndex, "Separator should appear before title");
    }

    #endregion

    #region DisplayEvaluation Tests

    /// <summary>
    /// Verifies that DisplayEvaluation outputs the evaluation ToString result.
    /// </summary>
    [Fact]
    public void DisplayEvaluation_WithValidEvaluation_DisplaysEvaluationString()
    {
        // Arrange
        var evaluation = CreateSampleEvaluation();

        // Act
        _presenter.DisplayEvaluation(evaluation);
        var output = GetOutput();

        // Assert
        Assert.Contains(evaluation.ToString(), output);
    }

    /// <summary>
    /// Verifies that DisplayEvaluation throws ArgumentNullException when evaluation is null.
    /// </summary>
    [Fact]
    public void DisplayEvaluation_WithNullEvaluation_ThrowsArgumentNullException()
    {
        // Arrange
        EvidenceHypothesisEvaluation? nullEvaluation = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _presenter.DisplayEvaluation(nullEvaluation!));
    }

    /// <summary>
    /// Verifies that DisplayEvaluation handles all consistency levels correctly.
    /// </summary>
    [Fact]
    public void DisplayEvaluation_WithDifferentConsistencyLevels_DisplaysCorrectly()
    {
        // Arrange
        var evaluations = new[]
        {
            new EvidenceHypothesisEvaluation { EvidenceId = Guid.NewGuid(), HypothesisId = Guid.NewGuid(), Consistency = Consistency.Consistent, Relevance = Relevance.VeryRelevant, Reasoning = "Test" },
            new EvidenceHypothesisEvaluation { EvidenceId = Guid.NewGuid(), HypothesisId = Guid.NewGuid(), Consistency = Consistency.Inconsistent, Relevance = Relevance.VeryRelevant, Reasoning = "Test" },
            new EvidenceHypothesisEvaluation { EvidenceId = Guid.NewGuid(), HypothesisId = Guid.NewGuid(), Consistency = Consistency.NotApplicable, Relevance = Relevance.VeryRelevant, Reasoning = "Test" }
        };

        // Act & Assert - Should not throw for any consistency level
        foreach (var evaluation in evaluations)
        {
            ClearOutput();
            _presenter.DisplayEvaluation(evaluation);
            var output = GetOutput();
            Assert.Contains(evaluation.ToString(), output);
        }
    }

    /// <summary>
    /// Verifies that DisplayEvaluation handles all relevance levels correctly.
    /// </summary>
    [Fact]
    public void DisplayEvaluation_WithDifferentRelevanceLevels_DisplaysCorrectly()
    {
        // Arrange
        var evaluations = new[]
        {
            new EvidenceHypothesisEvaluation { EvidenceId = Guid.NewGuid(), HypothesisId = Guid.NewGuid(), Consistency = Consistency.Consistent, Relevance = Relevance.VeryRelevant, Reasoning = "Test" },
            new EvidenceHypothesisEvaluation { EvidenceId = Guid.NewGuid(), HypothesisId = Guid.NewGuid(), Consistency = Consistency.Consistent, Relevance = Relevance.SomewhatRelevant, Reasoning = "Test" },
            new EvidenceHypothesisEvaluation { EvidenceId = Guid.NewGuid(), HypothesisId = Guid.NewGuid(), Consistency = Consistency.Consistent, Relevance = Relevance.NotRelevant, Reasoning = "Test" }
        };

        // Act & Assert - Should not throw for any relevance level
        foreach (var evaluation in evaluations)
        {
            ClearOutput();
            _presenter.DisplayEvaluation(evaluation);
            var output = GetOutput();
            Assert.Contains(evaluation.ToString(), output);
        }
    }

    #endregion

    #region DisplayErrorMessage Tests

    /// <summary>
    /// Verifies that DisplayErrorMessage displays the message with ERROR prefix.
    /// </summary>
    [Fact]
    public void DisplayErrorMessage_WithValidMessage_DisplaysWithErrorPrefix()
    {
        // Arrange
        var message = "Test error message";

        // Act
        _presenter.DisplayErrorMessage(message);
        var output = GetOutput();

        // Assert
        Assert.Contains("ERROR:", output);
        Assert.Contains(message, output);
    }

    /// <summary>
    /// Verifies that DisplayErrorMessage throws ArgumentException when message is null.
    /// </summary>
    [Fact]
    public void DisplayErrorMessage_WithNullMessage_ThrowsArgumentException()
    {
        // Arrange
        string? nullMessage = null;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayErrorMessage(nullMessage!));
    }

    /// <summary>
    /// Verifies that DisplayErrorMessage throws ArgumentException when message is empty.
    /// </summary>
    [Fact]
    public void DisplayErrorMessage_WithEmptyMessage_ThrowsArgumentException()
    {
        // Arrange
        var emptyMessage = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayErrorMessage(emptyMessage));
    }

    /// <summary>
    /// Verifies that DisplayErrorMessage handles multi-line error messages.
    /// </summary>
    [Fact]
    public void DisplayErrorMessage_WithMultilineMessage_DisplaysCorrectly()
    {
        // Arrange
        var multilineMessage = "Line 1\nLine 2\nLine 3";

        // Act
        _presenter.DisplayErrorMessage(multilineMessage);
        var output = GetOutput();

        // Assert
        Assert.Contains("ERROR:", output);
        Assert.Contains(multilineMessage, output);
    }

    /// <summary>
    /// Verifies that DisplayErrorMessage handles special characters in the message.
    /// </summary>
    [Fact]
    public void DisplayErrorMessage_WithSpecialCharacters_DisplaysCorrectly()
    {
        // Arrange
        var specialMessage = "Error: <>&\" 你好 🎉";

        // Act
        _presenter.DisplayErrorMessage(specialMessage);
        var output = GetOutput();

        // Assert
        Assert.Contains("ERROR:", output);
        Assert.Contains(specialMessage, output);
    }

    /// <summary>
    /// Verifies that DisplayErrorMessage handles very long error messages.
    /// </summary>
    [Fact]
    public void DisplayErrorMessage_WithVeryLongMessage_HandlesCorrectly()
    {
        // Arrange
        var longMessage = new string('X', 5000);

        // Act
        _presenter.DisplayErrorMessage(longMessage);
        var output = GetOutput();

        // Assert
        Assert.Contains("ERROR:", output);
        Assert.Contains(longMessage, output);
    }

    #endregion

    #region Interface Implementation Tests

    /// <summary>
    /// Verifies that ConsoleResultPresenter implements IResultPresenter interface.
    /// </summary>
    [Fact]
    public void ConsoleResultPresenter_ImplementsIResultPresenter()
    {
        // Assert
        Assert.IsAssignableFrom<NIU.ACH_AI.Application.Interfaces.IResultPresenter>(_presenter);
    }

    /// <summary>
    /// Verifies that all IResultPresenter methods are implemented.
    /// </summary>
    [Fact]
    public void ConsoleResultPresenter_ImplementsAllInterfaceMethods()
    {
        // Arrange
        var interfaceType = typeof(NIU.ACH_AI.Application.Interfaces.IResultPresenter);
        var implementationType = typeof(ConsoleResultPresenter);

        // Act
        var interfaceMethods = interfaceType.GetMethods();
        var implementationMethods = implementationType.GetMethods();

        // Assert
        foreach (var interfaceMethod in interfaceMethods)
        {
            var hasImplementation = implementationMethods.Any(m =>
                m.Name == interfaceMethod.Name &&
                m.ReturnType == interfaceMethod.ReturnType);
            Assert.True(hasImplementation, $"Method {interfaceMethod.Name} is not implemented");
        }
    }

    #endregion

    #region Integration and Edge Case Tests

    /// <summary>
    /// Verifies that multiple display operations can be performed sequentially.
    /// </summary>
    [Fact]
    public void Presenter_MultipleSequentialOperations_WorksCorrectly()
    {
        // Arrange
        var config = CreateSampleExperimentConfiguration();
        var hypotheses = CreateSampleHypotheses(2);
        var evidence = CreateSampleEvidence(2);
        var evaluation = CreateSampleEvaluation();

        // Act
        _presenter.DisplayExperimentInfo(config);
        _presenter.DisplayHypotheses("Test Hypotheses", hypotheses);
        _presenter.DisplayEvidence("Test Evidence", evidence);
        _presenter.DisplayEvaluation(evaluation);
        _presenter.DisplayErrorMessage("Test error");
        var output = GetOutput();

        // Assert - All outputs should be present
        Assert.Contains(config.ToString(), output);
        Assert.Contains("Test Hypotheses", output);
        Assert.Contains("Test Evidence", output);
        Assert.Contains(evaluation.ToString(), output);
        Assert.Contains("ERROR: Test error", output);
    }

    /// <summary>
    /// Verifies that the presenter uses consistent separator length across methods.
    /// </summary>
    [Fact]
    public void Presenter_AllMethods_UseConsistentSeparatorLength()
    {
        // Arrange
        var config = CreateSampleExperimentConfiguration();
        var hypotheses = CreateSampleHypotheses(1);
        var evidence = CreateSampleEvidence(1);
        var expectedSeparator = new string('=', 70);

        // Act
        _presenter.DisplayExperimentInfo(config);
        _presenter.DisplayHypotheses("Test", hypotheses);
        _presenter.DisplayEvidence("Test", evidence);
        var output = GetOutput();

        // Assert - All separators should be 70 characters
        var separatorCount = CountOccurrences(output, expectedSeparator);
        Assert.True(separatorCount >= 3, $"Expected at least 3 separators, found {separatorCount}");
    }

    /// <summary>
    /// Verifies that output is produced immediately (not buffered).
    /// </summary>
    [Fact]
    public void Presenter_Output_IsNotBuffered()
    {
        // Arrange
        var message = "Immediate output test";

        // Act
        _presenter.DisplayErrorMessage(message);
        var output = GetOutput();

        // Assert - Output should be available immediately
        Assert.Contains(message, output);
    }

    /// <summary>
    /// Verifies that display methods do not modify input collections.
    /// </summary>
    [Fact]
    public void DisplayMethods_DoNotModifyInputCollections()
    {
        // Arrange
        var hypotheses = CreateSampleHypotheses(3);
        var evidence = CreateSampleEvidence(3);
        var originalHypothesesCount = hypotheses.Count;
        var originalEvidenceCount = evidence.Count;

        // Act
        _presenter.DisplayHypotheses("Test", hypotheses);
        _presenter.DisplayEvidence("Test", evidence);

        // Assert
        Assert.Equal(originalHypothesesCount, hypotheses.Count);
        Assert.Equal(originalEvidenceCount, evidence.Count);
    }

    /// <summary>
    /// Verifies that whitespace-only titles are rejected by DisplayHypotheses.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithWhitespaceTitle_ThrowsArgumentException()
    {
        // Arrange
        var whitespaceTitle = "   ";
        var hypotheses = CreateSampleHypotheses(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayHypotheses(whitespaceTitle, hypotheses));
    }

    /// <summary>
    /// Verifies that whitespace-only titles are rejected by DisplayEvidence.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithWhitespaceTitle_ThrowsArgumentException()
    {
        // Arrange
        var whitespaceTitle = "   ";
        var evidence = CreateSampleEvidence(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayEvidence(whitespaceTitle, evidence));
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Counts the occurrences of a substring in a string.
    /// </summary>
    private int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    #endregion
}
