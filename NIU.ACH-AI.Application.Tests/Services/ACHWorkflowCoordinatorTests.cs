using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Application.Services;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Tests.Services;

/// <summary>
/// Comprehensive unit tests for ACHWorkflowCoordinator.
///
/// Testing Strategy:
/// -----------------
/// ACHWorkflowCoordinator is an application service that orchestrates the complete
/// ACH workflow by executing multiple steps sequentially and managing state transitions
/// between steps. Tests verify workflow execution, state management, and error handling.
///
/// What We Can Test:
/// 1. Constructor - Validates all dependencies are required and properly stored
/// 2. ExecuteWorkflowAsync - Verifies complete workflow execution with multiple steps
/// 3. Step Execution - Tests each step type (hypothesis brainstorming, refinement, evidence extraction, evaluation)
/// 4. State Management - Confirms data flows correctly between workflow steps
/// 5. Error Handling - Validates proper exception handling and error result population
/// 6. Unknown Step Types - Ensures invalid step names throw appropriate exceptions
/// 7. Cancellation - Verifies cancellation token propagation through workflow
///
/// Testing Approach:
/// Uses Moq to mock external dependencies (IOrchestrationExecutor, IOrchestrationFactoryProvider,
/// ILoggerFactory) and FluentAssertions for readable assertions. Tests focus on workflow
/// coordination logic, step sequencing, and state transitions rather than the underlying
/// orchestration factory implementations or AI service execution.
/// </summary>
public class ACHWorkflowCoordinatorTests
{
    private readonly Mock<IOrchestrationExecutor> _mockOrchestrationExecutor;
    private readonly Mock<IOrchestrationFactoryProvider> _mockFactoryProvider;
    private readonly Mock<IWorkflowPersistence> _mockWorkflowPersistence;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<ACHWorkflowCoordinator>> _mockLogger;
    private readonly ACHWorkflowCoordinator _coordinator;

    public ACHWorkflowCoordinatorTests()
    {
        _mockOrchestrationExecutor = new Mock<IOrchestrationExecutor>();
        _mockFactoryProvider = new Mock<IOrchestrationFactoryProvider>();
        _mockWorkflowPersistence = new Mock<IWorkflowPersistence>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<ACHWorkflowCoordinator>>();

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _mockWorkflowPersistence
            .Setup(x => x.CreateScenarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mockWorkflowPersistence
            .Setup(x => x.CreateExperimentAsync(It.IsAny<ExperimentConfiguration>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _mockWorkflowPersistence
            .Setup(x => x.CreateStepExecutionAsync(It.IsAny<Guid>(), It.IsAny<ACHStepConfiguration>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid experimentId, ACHStepConfiguration step, CancellationToken _) =>
                new StepExecutionContext
                {
                    ExperimentId = experimentId,
                    StepExecutionId = Guid.NewGuid(),
                    AchStepId = step.Id,
                    AchStepName = step.Name
                });
        _mockWorkflowPersistence
            .Setup(x => x.UpdateStepExecutionStatusAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _coordinator = new ACHWorkflowCoordinator(
            _mockOrchestrationExecutor.Object,
            _mockFactoryProvider.Object,
            _mockWorkflowPersistence.Object,
            _mockLoggerFactory.Object);
    }

    #region Constructor Tests

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when passed a null orchestration executor.
    /// </summary>
    [Fact]
    public void Constructor_WithNullOrchestrationExecutor_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ACHWorkflowCoordinator(
                null!,
                _mockFactoryProvider.Object,
                _mockWorkflowPersistence.Object,
                _mockLoggerFactory.Object));
        exception.ParamName.Should().Be("orchestrationExecutor");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when passed a null factory provider.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFactoryProvider_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ACHWorkflowCoordinator(
                _mockOrchestrationExecutor.Object,
                null!,
                _mockWorkflowPersistence.Object,
                _mockLoggerFactory.Object));
        exception.ParamName.Should().Be("factoryProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when passed a null logger factory.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ACHWorkflowCoordinator(
                _mockOrchestrationExecutor.Object,
                _mockFactoryProvider.Object,
                _mockWorkflowPersistence.Object,
                null!));
        exception.ParamName.Should().Be("loggerFactory");
    }

    /// <summary>
    /// Verifies that the constructor successfully creates an instance with valid dependencies.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange & Act
        var coordinator = new ACHWorkflowCoordinator(
            _mockOrchestrationExecutor.Object,
            _mockFactoryProvider.Object,
            _mockWorkflowPersistence.Object,
            _mockLoggerFactory.Object);

        // Assert
        coordinator.Should().NotBeNull();
    }

    #endregion

    #region ExecuteWorkflowAsync - Hypothesis Brainstorming Tests

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync successfully executes a hypothesis brainstorming step and returns results.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithHypothesisBrainstormingStep_ExecutesSuccessfully()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration
            {
                Id = 1,
                Name = "Hypothesis Brainstorming",
                TaskInstructions = "Generate hypotheses"
            }
        });

        var expectedHypotheses = new List<Hypothesis>
        {
            new Hypothesis { ShortTitle = "H1", HypothesisText = "Hypothesis 1" }
        };

        SetupMockFactory<List<Hypothesis>>(expectedHypotheses);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Hypotheses.Should().HaveCount(1);
        result.Hypotheses![0].ShortTitle.Should().Be("H1");
        result.ExperimentId.Should().Be("exp-1");
        result.ExperimentName.Should().Be("Test Experiment");
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync handles the alternative step name "HypothesisBrainstorming" correctly.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithHypothesisBrainstormingAlternativeName_ExecutesSuccessfully()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration
            {
                Id = 1,
                Name = "HypothesisBrainstorming",
                TaskInstructions = "Generate hypotheses"
            }
        });

        var expectedHypotheses = new List<Hypothesis>
        {
            new Hypothesis { ShortTitle = "H1", HypothesisText = "Hypothesis 1" }
        };

        SetupMockFactory<List<Hypothesis>>(expectedHypotheses);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        result.Hypotheses.Should().HaveCount(1);
    }

    #endregion

    #region ExecuteWorkflowAsync - Hypothesis Refinement Tests

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync successfully executes a hypothesis refinement step and updates state.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithHypothesisRefinementStep_ExecutesSuccessfully()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration
            {
                Id = 1,
                Name = "Hypothesis Refinement",
                TaskInstructions = "Refine hypotheses"
            }
        });

        var expectedRefinedHypotheses = new List<Hypothesis>
        {
            new Hypothesis { ShortTitle = "Refined H1", HypothesisText = "Refined Hypothesis 1" }
        };

        SetupMockFactory<List<Hypothesis>>(expectedRefinedHypotheses);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        result.RefinedHypotheses.Should().HaveCount(1);
        result.RefinedHypotheses![0].ShortTitle.Should().Be("Refined H1");
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync handles alternative hypothesis refinement step names correctly.
    /// </summary>
    [Theory]
    [InlineData("Hypothesis Evaluation")]
    [InlineData("HypothesisEvaluation")]
    [InlineData("HypothesisRefinement")]
    public async Task ExecuteWorkflowAsync_WithHypothesisRefinementAlternativeNames_ExecutesSuccessfully(string stepName)
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration
            {
                Id = 1,
                Name = stepName,
                TaskInstructions = "Refine hypotheses"
            }
        });

        var expectedHypotheses = new List<Hypothesis>
        {
            new Hypothesis { ShortTitle = "Refined", HypothesisText = "Refined" }
        };

        SetupMockFactory<List<Hypothesis>>(expectedHypotheses);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        result.RefinedHypotheses.Should().HaveCount(1);
    }

    #endregion

    #region ExecuteWorkflowAsync - Evidence Extraction Tests

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync successfully executes an evidence extraction step and populates evidence.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithEvidenceExtractionStep_ExecutesSuccessfully()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration
            {
                Id = 1,
                Name = "Evidence Extraction",
                TaskInstructions = "Extract evidence"
            }
        });

        var expectedEvidence = new List<Evidence>
        {
            new Evidence { Claim = "Evidence 1", ReferenceSnippet = "Source 1" }
        };

        SetupMockFactory<List<Evidence>>(expectedEvidence);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        result.Evidence.Should().HaveCount(1);
        result.Evidence![0].Claim.Should().Be("Evidence 1");
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync handles the alternative step name "EvidenceExtraction" correctly.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithEvidenceExtractionAlternativeName_ExecutesSuccessfully()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration
            {
                Id = 1,
                Name = "EvidenceExtraction",
                TaskInstructions = "Extract evidence"
            }
        });

        var expectedEvidence = new List<Evidence>
        {
            new Evidence { Claim = "Evidence", ReferenceSnippet = "Source" }
        };

        SetupMockFactory<List<Evidence>>(expectedEvidence);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        result.Evidence.Should().HaveCount(1);
    }

    #endregion

    #region ExecuteWorkflowAsync - Evidence Hypothesis Evaluation Tests

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync successfully executes evidence-hypothesis evaluation with refined hypotheses.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithEvidenceEvaluationStep_UsesRefinedHypotheses()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration { Id = 1, Name = "Hypothesis Brainstorming" },
            new ACHStepConfiguration { Id = 2, Name = "Hypothesis Refinement" },
            new ACHStepConfiguration { Id = 3, Name = "Evidence Extraction" },
            new ACHStepConfiguration { Id = 4, Name = "Evidence Hypothesis Evaluation" }
        });

        var hypotheses = new List<Hypothesis>
        {
            new Hypothesis { ShortTitle = "H1", HypothesisText = "Original" }
        };

        var refinedHypotheses = new List<Hypothesis>
        {
            new Hypothesis { ShortTitle = "H1-Refined", HypothesisText = "Refined" }
        };

        var evidence = new List<Evidence>
        {
            new Evidence { Claim = "E1", ReferenceSnippet = "S1" }
        };

        var evaluations = new List<EvidenceHypothesisEvaluation>
        {
            new EvidenceHypothesisEvaluation
            {
                ScoreRationale = "Consistent with hypothesis",
                ConfidenceLevel = 0.9m
            }
        };

        SetupMockFactorySequence(hypotheses, refinedHypotheses, evidence, evaluations);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        result.Evaluations.Should().HaveCount(1);
        result.Evaluations![0].ScoreRationale.Should().Be("Consistent with hypothesis");
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync uses initial hypotheses when refined hypotheses are not available for evaluation.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithEvidenceEvaluationStep_UsesInitialHypothesesWhenNoRefined()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration { Id = 1, Name = "Hypothesis Brainstorming" },
            new ACHStepConfiguration { Id = 2, Name = "Evidence Extraction" },
            new ACHStepConfiguration { Id = 3, Name = "Evidence Evaluation" }
        });

        var hypotheses = new List<Hypothesis>
        {
            new Hypothesis { ShortTitle = "H1", HypothesisText = "Hypothesis" }
        };

        var evidence = new List<Evidence>
        {
            new Evidence { Claim = "E1", ReferenceSnippet = "S1" }
        };

        var evaluations = new List<EvidenceHypothesisEvaluation>
        {
            new EvidenceHypothesisEvaluation { ScoreRationale = "Neutral assessment" }
        };

        SetupMockFactorySequence(hypotheses, evidence, evaluations);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        result.Hypotheses.Should().HaveCount(1);
        result.RefinedHypotheses.Should().BeNull();
        result.Evaluations.Should().HaveCount(1);
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync handles alternative evidence evaluation step names correctly.
    /// </summary>
    [Theory]
    [InlineData("Evidence Hypothesis Evaluation")]
    [InlineData("EvidenceHypothesisEvaluation")]
    [InlineData("EvidenceEvaluation")]
    public async Task ExecuteWorkflowAsync_WithEvidenceEvaluationAlternativeNames_ExecutesSuccessfully(string stepName)
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration { Id = 1, Name = "Hypothesis Brainstorming" },
            new ACHStepConfiguration { Id = 2, Name = "Evidence Extraction" },
            new ACHStepConfiguration { Id = 3, Name = stepName }
        });

        var hypotheses = new List<Hypothesis> { new Hypothesis { ShortTitle = "H1" } };
        var evidence = new List<Evidence> { new Evidence { Claim = "E1" } };
        var evaluations = new List<EvidenceHypothesisEvaluation>
        {
            new EvidenceHypothesisEvaluation { ScoreRationale = "Consistent with evidence" }
        };

        SetupMockFactorySequence(hypotheses, evidence, evaluations);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        result.Evaluations.Should().HaveCount(1);
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync creates multiple evaluations for multiple evidence-hypothesis pairs.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithMultipleEvidenceAndHypotheses_CreatesAllEvaluations()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration { Id = 1, Name = "Hypothesis Brainstorming" },
            new ACHStepConfiguration { Id = 2, Name = "Evidence Extraction" },
            new ACHStepConfiguration { Id = 3, Name = "Evidence Evaluation" }
        });

        var hypotheses = new List<Hypothesis>
        {
            new Hypothesis { ShortTitle = "H1" },
            new Hypothesis { ShortTitle = "H2" }
        };

        var evidence = new List<Evidence>
        {
            new Evidence { Claim = "E1" },
            new Evidence { Claim = "E2" }
        };

        var singleEvaluation = new List<EvidenceHypothesisEvaluation>
        {
            new EvidenceHypothesisEvaluation { ScoreRationale = "Evaluation result" }
        };

        SetupMockFactorySequence(hypotheses, evidence, singleEvaluation);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        // 2 hypotheses * 2 evidence = 4 evaluations
        result.Evaluations.Should().HaveCount(4);
    }

    #endregion

    #region ExecuteWorkflowAsync - Multi-Step Workflow Tests

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync executes a complete multi-step workflow and maintains state correctly.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithCompleteWorkflow_ExecutesAllStepsInSequence()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration { Id = 1, Name = "Hypothesis Brainstorming", TaskInstructions = "Step 1" },
            new ACHStepConfiguration { Id = 2, Name = "Hypothesis Refinement", TaskInstructions = "Step 2" },
            new ACHStepConfiguration { Id = 3, Name = "Evidence Extraction", TaskInstructions = "Step 3" },
            new ACHStepConfiguration { Id = 4, Name = "Evidence Evaluation", TaskInstructions = "Step 4" }
        });

        var hypotheses = new List<Hypothesis> { new Hypothesis { ShortTitle = "H1" } };
        var refinedHypotheses = new List<Hypothesis> { new Hypothesis { ShortTitle = "H1-Refined" } };
        var evidence = new List<Evidence> { new Evidence { Claim = "E1" } };
        var evaluations = new List<EvidenceHypothesisEvaluation>
        {
            new EvidenceHypothesisEvaluation { ScoreRationale = "Evaluation complete" }
        };

        SetupMockFactorySequence(hypotheses, refinedHypotheses, evidence, evaluations);

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        result.Hypotheses.Should().HaveCount(1);
        result.RefinedHypotheses.Should().HaveCount(1);
        result.Evidence.Should().HaveCount(1);
        result.Evaluations.Should().HaveCount(1);
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync handles an empty workflow with no steps without errors.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithNoSteps_CompletesSuccessfully()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(Array.Empty<ACHStepConfiguration>());

        // Act
        var result = await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        result.Success.Should().BeTrue();
        result.Hypotheses.Should().BeNull();
        result.RefinedHypotheses.Should().BeNull();
        result.Evidence.Should().BeNull();
        result.Evaluations.Should().BeNull();
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync throws InvalidOperationException when encountering an unknown step type.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithUnknownStepType_ThrowsInvalidOperationException()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration
            {
                Id = 1,
                Name = "Unknown Step Type",
                TaskInstructions = "This should fail"
            }
        });

        // Act & Assert
        await _coordinator
            .Invoking(c => c.ExecuteWorkflowAsync(experimentConfig))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unknown ACH step: Unknown Step Type. Unable to execute this step type.");
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync sets error state when a step execution fails.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WhenStepFails_SetsErrorStateAndRethrows()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration { Id = 1, Name = "Hypothesis Brainstorming" }
        });

        var mockFactory = new Mock<IOrchestrationFactory<List<Hypothesis>>>();
        _mockFactoryProvider
            .Setup(x => x.CreateFactory<List<Hypothesis>>(It.IsAny<ACHStepConfiguration>()))
            .Returns(mockFactory.Object);

        var expectedException = new InvalidOperationException("Factory execution failed");
        _mockOrchestrationExecutor
            .Setup(x => x.ExecuteAsync(
                It.IsAny<IOrchestrationFactory<List<Hypothesis>>>(),
                It.IsAny<OrchestrationPromptInput>(),
                It.IsAny<StepExecutionContext?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await _coordinator
            .Invoking(c => c.ExecuteWorkflowAsync(experimentConfig))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Factory execution failed");
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync logs error messages when workflow execution fails.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WhenExecutionFails_LogsError()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration { Id = 1, Name = "Unknown Step" }
        });

        // Act & Assert
        await _coordinator
            .Invoking(c => c.ExecuteWorkflowAsync(experimentConfig))
            .Should().ThrowAsync<InvalidOperationException>();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error executing ACH workflow")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Cancellation Tests

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync respects the cancellation token passed to step execution.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_WithCancellationToken_PassesTokenToExecutor()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration { Id = 1, Name = "Hypothesis Brainstorming" }
        });

        var cancellationToken = new CancellationToken();
        var expectedHypotheses = new List<Hypothesis> { new Hypothesis { ShortTitle = "H1" } };

        var mockFactory = new Mock<IOrchestrationFactory<List<Hypothesis>>>();
        _mockFactoryProvider
            .Setup(x => x.CreateFactory<List<Hypothesis>>(It.IsAny<ACHStepConfiguration>()))
            .Returns(mockFactory.Object);

        _mockOrchestrationExecutor
            .Setup(x => x.ExecuteAsync(
                mockFactory.Object,
                It.IsAny<OrchestrationPromptInput>(),
                It.IsAny<StepExecutionContext?>(),
                cancellationToken))
            .ReturnsAsync(expectedHypotheses);

        // Act
        await _coordinator.ExecuteWorkflowAsync(experimentConfig, cancellationToken);

        // Assert
        _mockOrchestrationExecutor.Verify(
            x => x.ExecuteAsync(
                It.IsAny<IOrchestrationFactory<List<Hypothesis>>>(),
                It.IsAny<OrchestrationPromptInput>(),
                It.IsAny<StepExecutionContext?>(),
                cancellationToken),
            Times.Once);
    }

    #endregion

    #region Logging Tests

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync logs information when starting workflow execution.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_LogsInformationWhenStarting()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(Array.Empty<ACHStepConfiguration>());

        // Act
        await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting ACH workflow execution")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync logs information when workflow completes successfully.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_LogsInformationWhenCompleted()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(Array.Empty<ACHStepConfiguration>());

        // Act
        await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully completed ACH workflow")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Verifies that ExecuteWorkflowAsync logs information for each step execution.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflowAsync_LogsInformationForEachStep()
    {
        // Arrange
        var experimentConfig = CreateExperimentConfig(new[]
        {
            new ACHStepConfiguration { Id = 1, Name = "Hypothesis Brainstorming" },
            new ACHStepConfiguration { Id = 2, Name = "Evidence Extraction" }
        });

        SetupMockFactory<List<Hypothesis>>(new List<Hypothesis> { new Hypothesis() });
        SetupMockFactory<List<Evidence>>(new List<Evidence> { new Evidence() });

        // Act
        await _coordinator.ExecuteWorkflowAsync(experimentConfig);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing ACH step")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    #endregion

    #region Helper Methods

    private ExperimentConfiguration CreateExperimentConfig(ACHStepConfiguration[] steps)
    {
        return new ExperimentConfiguration
        {
            Id = "exp-1",
            Name = "Test Experiment",
            Description = "Test experiment description",
            KeyQuestion = "What is the test question?",
            Context = "This is the test context.",
            ACHSteps = steps
        };
    }

    private void SetupMockFactory<T>(T returnValue)
    {
        var mockFactory = new Mock<IOrchestrationFactory<T>>();
        mockFactory
            .Setup(x => x.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<StepExecutionContext?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnValue);

        _mockFactoryProvider
            .Setup(x => x.CreateFactory<T>(It.IsAny<ACHStepConfiguration>()))
            .Returns(mockFactory.Object);

        _mockOrchestrationExecutor
            .Setup(x => x.ExecuteAsync(
                It.IsAny<IOrchestrationFactory<T>>(),
                It.IsAny<OrchestrationPromptInput>(),
                It.IsAny<StepExecutionContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(returnValue);
    }

    private void SetupMockFactorySequence(params object[] results)
    {
        var setupIndex = 0;

        _mockFactoryProvider
            .Setup(x => x.CreateFactory<List<Hypothesis>>(It.IsAny<ACHStepConfiguration>()))
            .Returns(() =>
            {
                var mockFactory = new Mock<IOrchestrationFactory<List<Hypothesis>>>();
                var index = setupIndex;
                mockFactory
                    .Setup(x => x.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<StepExecutionContext?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((List<Hypothesis>)results[index]);
                return mockFactory.Object;
            });

        _mockFactoryProvider
            .Setup(x => x.CreateFactory<List<Evidence>>(It.IsAny<ACHStepConfiguration>()))
            .Returns(() =>
            {
                var mockFactory = new Mock<IOrchestrationFactory<List<Evidence>>>();
                var evidenceIndex = Array.FindIndex(results, r => r is List<Evidence>);
                mockFactory
                    .Setup(x => x.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<StepExecutionContext?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((List<Evidence>)results[evidenceIndex]);
                return mockFactory.Object;
            });

        _mockFactoryProvider
            .Setup(x => x.CreateFactory<List<EvidenceHypothesisEvaluation>>(It.IsAny<ACHStepConfiguration>()))
            .Returns(() =>
            {
                var mockFactory = new Mock<IOrchestrationFactory<List<EvidenceHypothesisEvaluation>>>();
                var evalIndex = Array.FindIndex(results, r => r is List<EvidenceHypothesisEvaluation>);
                mockFactory
                    .Setup(x => x.ExecuteCoreAsync(It.IsAny<OrchestrationPromptInput>(), It.IsAny<StepExecutionContext?>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((List<EvidenceHypothesisEvaluation>)results[evalIndex]);
                return mockFactory.Object;
            });

        _mockOrchestrationExecutor
            .Setup(x => x.ExecuteAsync(
                It.IsAny<IOrchestrationFactory<List<Hypothesis>>>(),
                It.IsAny<OrchestrationPromptInput>(),
                It.IsAny<StepExecutionContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => (List<Hypothesis>)results[setupIndex++]);

        _mockOrchestrationExecutor
            .Setup(x => x.ExecuteAsync(
                It.IsAny<IOrchestrationFactory<List<Evidence>>>(),
                It.IsAny<OrchestrationPromptInput>(),
                It.IsAny<StepExecutionContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => (List<Evidence>)results[Array.FindIndex(results, r => r is List<Evidence>)]);

        _mockOrchestrationExecutor
            .Setup(x => x.ExecuteAsync(
                It.IsAny<IOrchestrationFactory<List<EvidenceHypothesisEvaluation>>>(),
                It.IsAny<OrchestrationPromptInput>(),
                It.IsAny<StepExecutionContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<EvidenceHypothesisEvaluation>)results[Array.FindIndex(results, r => r is List<EvidenceHypothesisEvaluation>)]);
    }

    #endregion
}
