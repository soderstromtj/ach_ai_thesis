using System.Text;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Domain.ValueObjects;
using NIU.ACH_AI.FrontendConsole.Presentation;

namespace NIU.ACH_AI.FrontendConsole.Tests;

/// <summary>
/// Comprehensive unit tests for ConsoleResultPresenter.
///
/// Testing Strategy:
/// -----------------
/// ConsoleResultPresenter is a CONCRETE class that writes formatted ACH results to the console.
/// Tests redirect Console.Out to a StringWriter so output can be captured and asserted without
/// real console I/O.
///
/// What We Can Test:
/// 1. DisplayExperimentInfo - Validates formatting and required input checks
/// 2. DisplayHypotheses - Validates formatting and required input checks
/// 3. DisplayEvidence - Validates formatting and required input checks
/// 4. DisplayEvaluation - Validates formatting and required input checks
/// 5. DisplayErrorMessage - Validates formatting and required input checks
/// 6. Interface Contract - Confirms IResultPresenter implementation
///
/// Testing Challenges:
/// Console output is side-effect driven, so tests must capture output and avoid ordering issues
/// by clearing the buffer between assertions.
/// </summary>
public class ConsoleResultPresenterTests : IDisposable
{
    private readonly ConsoleResultPresenter _presenter;
    private readonly StringWriter _stringWriter;
    private readonly TextWriter _originalOutput;

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

    private string GetOutput() => _stringWriter.ToString();

    private void ClearOutput() => _stringWriter.GetStringBuilder().Clear();

    #region DisplayExperimentInfo Tests

    /// <summary>
    /// This test verifies DisplayExperimentInfo writes the configuration and a separator.
    /// </summary>
    [Fact]
    public void DisplayExperimentInfo_WithValidConfiguration_WritesConfigurationAndSeparator()
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
    /// This test verifies DisplayExperimentInfo throws when the configuration is null.
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
    /// This test verifies DisplayExperimentInfo throws when the configuration name is empty.
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
    /// This test verifies DisplayExperimentInfo throws when the key question is empty.
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

    #endregion

    #region DisplayHypotheses Tests

    /// <summary>
    /// This test verifies DisplayHypotheses writes the title, separator, and each hypothesis.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithValidInput_WritesTitleSeparatorAndItems()
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
    /// This test verifies DisplayHypotheses throws when the title is null.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithNullTitle_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullTitle = null;
        var hypotheses = CreateSampleHypotheses(1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _presenter.DisplayHypotheses(nullTitle!, hypotheses));
    }

    /// <summary>
    /// This test verifies DisplayHypotheses throws when the title is whitespace.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithWhitespaceTitle_ThrowsArgumentException()
    {
        // Arrange
        var hypotheses = CreateSampleHypotheses(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayHypotheses("   ", hypotheses));
    }

    /// <summary>
    /// This test verifies DisplayHypotheses throws when the hypotheses collection is null.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<Hypothesis>? nullHypotheses = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _presenter.DisplayHypotheses("Test", nullHypotheses!));
    }

    /// <summary>
    /// This test verifies DisplayHypotheses writes the title and separator for an empty list.
    /// </summary>
    [Fact]
    public void DisplayHypotheses_WithEmptyCollection_WritesTitleAndSeparatorOnly()
    {
        // Arrange
        var hypotheses = new List<Hypothesis>();

        // Act
        _presenter.DisplayHypotheses("Empty", hypotheses);
        var output = GetOutput();

        // Assert
        Assert.Contains("Empty", output);
        Assert.Contains(new string('=', 70), output);
    }

    #endregion

    #region DisplayEvidence Tests

    /// <summary>
    /// This test verifies DisplayEvidence writes the title, separator, and each evidence item.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithValidInput_WritesTitleSeparatorAndItems()
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
        foreach (var item in evidence)
        {
            Assert.Contains(item.ToString(), output);
        }
    }

    /// <summary>
    /// This test verifies DisplayEvidence throws when the title is null.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithNullTitle_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullTitle = null;
        var evidence = CreateSampleEvidence(1);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _presenter.DisplayEvidence(nullTitle!, evidence));
    }

    /// <summary>
    /// This test verifies DisplayEvidence throws when the title is whitespace.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithWhitespaceTitle_ThrowsArgumentException()
    {
        // Arrange
        var evidence = CreateSampleEvidence(1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayEvidence("   ", evidence));
    }

    /// <summary>
    /// This test verifies DisplayEvidence throws when the evidence collection is null.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<Evidence>? nullEvidence = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _presenter.DisplayEvidence("Test", nullEvidence!));
    }

    /// <summary>
    /// This test verifies DisplayEvidence writes the title and separator for an empty list.
    /// </summary>
    [Fact]
    public void DisplayEvidence_WithEmptyCollection_WritesTitleAndSeparatorOnly()
    {
        // Arrange
        var evidence = new List<Evidence>();

        // Act
        _presenter.DisplayEvidence("Empty", evidence);
        var output = GetOutput();

        // Assert
        Assert.Contains("Empty", output);
        Assert.Contains(new string('=', 70), output);
    }

    #endregion

    #region DisplayEvaluation Tests

    /// <summary>
    /// This test verifies DisplayEvaluation writes the evaluation string output.
    /// </summary>
    [Fact]
    public void DisplayEvaluation_WithValidEvaluation_WritesEvaluationString()
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
    /// This test verifies DisplayEvaluation throws when the evaluation is null.
    /// </summary>
    [Fact]
    public void DisplayEvaluation_WithNullEvaluation_ThrowsArgumentNullException()
    {
        // Arrange
        EvidenceHypothesisEvaluation? nullEvaluation = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _presenter.DisplayEvaluation(nullEvaluation!));
    }

    #endregion

    #region DisplayErrorMessage Tests

    /// <summary>
    /// This test verifies DisplayErrorMessage writes the error prefix and message.
    /// </summary>
    [Fact]
    public void DisplayErrorMessage_WithValidMessage_WritesErrorPrefixAndMessage()
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
    /// This test verifies DisplayErrorMessage throws when the message is null.
    /// </summary>
    [Fact]
    public void DisplayErrorMessage_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullMessage = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _presenter.DisplayErrorMessage(nullMessage!));
    }

    /// <summary>
    /// This test verifies DisplayErrorMessage throws when the message is empty.
    /// </summary>
    [Fact]
    public void DisplayErrorMessage_WithEmptyMessage_ThrowsArgumentException()
    {
        // Arrange
        var emptyMessage = string.Empty;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _presenter.DisplayErrorMessage(emptyMessage));
    }

    #endregion

    #region Interface and Integration Tests

    /// <summary>
    /// This test verifies ConsoleResultPresenter implements the IResultPresenter interface.
    /// </summary>
    [Fact]
    public void ConsoleResultPresenter_ImplementsIResultPresenter()
    {
        // Assert
        Assert.IsAssignableFrom<NIU.ACH_AI.Application.Interfaces.IResultPresenter>(_presenter);
    }

    /// <summary>
    /// This test verifies multiple display operations produce combined output.
    /// </summary>
    [Fact]
    public void Presenter_WithSequentialCalls_WritesAllSections()
    {
        // Arrange
        var config = CreateSampleExperimentConfiguration();
        var hypotheses = CreateSampleHypotheses(1);
        var evidence = CreateSampleEvidence(1);
        var evaluation = CreateSampleEvaluation();

        // Act
        _presenter.DisplayExperimentInfo(config);
        _presenter.DisplayHypotheses("Hypotheses", hypotheses);
        _presenter.DisplayEvidence("Evidence", evidence);
        _presenter.DisplayEvaluation(evaluation);
        _presenter.DisplayErrorMessage("Test error");
        var output = GetOutput();

        // Assert
        Assert.Contains(config.ToString(), output);
        Assert.Contains("Hypotheses", output);
        Assert.Contains("Evidence", output);
        Assert.Contains(evaluation.ToString(), output);
        Assert.Contains("ERROR: Test error", output);
    }

    /// <summary>
    /// This test verifies display methods do not mutate input collections.
    /// </summary>
    [Fact]
    public void DisplayMethods_DoNotModifyInputCollections()
    {
        // Arrange
        var hypotheses = CreateSampleHypotheses(2);
        var evidence = CreateSampleEvidence(2);
        var hypothesesCount = hypotheses.Count;
        var evidenceCount = evidence.Count;

        // Act
        _presenter.DisplayHypotheses("Test", hypotheses);
        _presenter.DisplayEvidence("Test", evidence);

        // Assert
        Assert.Equal(hypothesesCount, hypotheses.Count);
        Assert.Equal(evidenceCount, evidence.Count);
    }

    #endregion

    #region Helpers

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
                    Id = 1,
                    Name = "Brainstorming",
                    Description = "Brainstorm hypotheses",
                    TaskInstructions = "Generate hypotheses",
                    AgentConfigurations = Array.Empty<AgentConfiguration>(),
                    OrchestrationSettings = new OrchestrationSettings()
                }
            }
        };
    }

    private static List<Hypothesis> CreateSampleHypotheses(int count)
    {
        var hypotheses = new List<Hypothesis>();
        for (int i = 0; i < count; i++)
        {
            hypotheses.Add(new Hypothesis
            {
                ShortTitle = $"Hypothesis {i + 1}",
                HypothesisText = $"Full text of hypothesis {i + 1}"
            });
        }
        return hypotheses;
    }

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
                Type = i % 2 == 0 ? EvidenceType.VerifiableFact : EvidenceType.StatedAssumption,
                Notes = $"Notes {i + 1}"
            });
        }
        return evidence;
    }

    private static EvidenceHypothesisEvaluation CreateSampleEvaluation()
    {
        return new EvidenceHypothesisEvaluation
        {
            Hypothesis = new Hypothesis { ShortTitle = "Test Hypothesis", HypothesisText = "Test hypothesis text" },
            Evidence = new Evidence { EvidenceId = Guid.NewGuid(), Claim = "Test claim", Type = EvidenceType.VerifiableFact },
            Score = EvaluationScore.Consistent,
            ScoreRationale = "Test score rationale",
            ConfidenceLevel = 0.85m,
            ConfidenceRationale = "Test confidence rationale"
        };
    }

    #endregion
}
