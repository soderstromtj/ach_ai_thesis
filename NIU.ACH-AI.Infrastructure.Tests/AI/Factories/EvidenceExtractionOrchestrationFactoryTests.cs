using Microsoft.Extensions.DependencyInjection; // Added
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
using Microsoft.SemanticKernel.Agents.Orchestration.Transforms;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Moq;
using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Application.DTOs;
using NIU.ACH_AI.Application.Interfaces;
using NIU.ACH_AI.Domain.Entities;
using NIU.ACH_AI.Infrastructure.AI.Factories;

namespace NIU.ACH_AI.Infrastructure.Tests.AI.Factories;

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only

/// <summary>
/// Unit tests for EvidenceExtractionOrchestrationFactory.
/// </summary>
public class EvidenceExtractionOrchestrationFactoryTests
{
    private readonly Mock<IAgentService> _agentServiceMock;
    private readonly Mock<IKernelBuilderService> _kernelBuilderServiceMock;
    private readonly Mock<IOptions<OrchestrationSettings>> _optionsMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<IAgentResponsePersistence> _agentResponsePersistenceMock;
    
    public EvidenceExtractionOrchestrationFactoryTests()
    {
        _agentServiceMock = new Mock<IAgentService>();
        _kernelBuilderServiceMock = new Mock<IKernelBuilderService>();
        _optionsMock = new Mock<IOptions<OrchestrationSettings>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _agentResponsePersistenceMock = new Mock<IAgentResponsePersistence>();
        
        // Setup default options
        _optionsMock.Setup(o => o.Value).Returns(new OrchestrationSettings());
        
        // Setup default logger
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
    }

    private TestableEvidenceExtractionOrchestrationFactory CreateFactory(bool includePersistence = false)
    {
        return new TestableEvidenceExtractionOrchestrationFactory(
            _agentServiceMock.Object,
            _kernelBuilderServiceMock.Object,
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            includePersistence ? _agentResponsePersistenceMock.Object : null
        );
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        var factory = CreateFactory();
        Assert.NotNull(factory);
    }

    [Fact]
    public void Constructor_NullArguments_ThrowsException()
    {
        var factory = new EvidenceExtractionOrchestrationFactory(
             _agentServiceMock.Object,
            _kernelBuilderServiceMock.Object,
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            null);
            
        Assert.NotNull(factory);
    }

    #endregion

    #region Protected Method Tests (via Testable Wrapper)

    [Fact]
    public void GetResultTypeName_ReturnsCorrectName()
    {
        var factory = CreateFactory();
        var result = factory.TestGetResultTypeName();
        Assert.Equal("EvidenceResult", result);
    }

    [Fact]
    public void UnwrapResult_ReturnsEvidenceList()
    {
        var factory = CreateFactory();
        var expectedEvidence = new List<Evidence> { new Evidence { Claim = "Test" } };
        var wrapper = new EvidenceResult { Evidence = expectedEvidence };

        var result = factory.TestUnwrapResult(wrapper);

        Assert.Same(expectedEvidence, result);
    }
    
    [Fact]
    public void UnwrapResult_WithEmptyList_ReturnsEmptyList()
    {
        var factory = CreateFactory();
        var wrapper = new EvidenceResult { Evidence = new List<Evidence>() };

        var result = factory.TestUnwrapResult(wrapper);

        Assert.Empty(result);
    }

    [Fact]
    public void GetItemCount_ReturnsCorrectCount()
    {
        var factory = CreateFactory();
        var list = new List<Evidence> { new Evidence(), new Evidence() };

        var count = factory.TestGetItemCount(list);

        Assert.Equal(2, count);
    }
    
    [Fact]
    public void GetItemCount_WithEmptyList_ReturnsZero()
    {
        var factory = CreateFactory();
        var list = new List<Evidence>();

        var count = factory.TestGetItemCount(list);

        Assert.Equal(0, count);
    }

    [Fact]
    public void CreateEmptyResult_ReturnsEmptyList()
    {
        var factory = CreateFactory();
        var result = factory.TestCreateEmptyResult();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void CreateErrorResult_ReturnsEmptyList()
    {
        var factory = CreateFactory();
        var result = factory.TestCreateErrorResult();
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetAgentSelectionReason_ReturnsStaticString()
    {
        var factory = CreateFactory();
        var reason = factory.TestGetAgentSelectionReason("AnyAgent");
        Assert.Equal("Group chat execution - agents selected by manager", reason);
    }

    #endregion

    #region CreateOrchestration Tests

    [Fact]
    public void CreateOrchestration_ConfiguresCorrectly()
    {
        // Arrange
        var factory = CreateFactory();
        var input = new OrchestrationPromptInput { KeyQuestion = "Test" };
        var agents = new Agent[] { new Mock<Agent>().Object };
        
        // Kernel setup using real ServiceProvider locally
        var chatCompletionMock = new Mock<IChatCompletionService>();
        
        var services = new ServiceCollection();
        services.AddSingleton(chatCompletionMock.Object);
        
        // Register mock filters to satisfy Kernel dependencies
        var functionFilterMock = new Mock<IFunctionInvocationFilter>();
        services.AddSingleton(functionFilterMock.Object);
        
        var promptFilterMock = new Mock<IPromptRenderFilter>();
        services.AddSingleton(promptFilterMock.Object);
        
        // Also register KernelPluginCollection if needed, or rely on default
        services.AddSingleton(new KernelPluginCollection());

        var provider = services.BuildServiceProvider();
        
        var kernel = new Kernel(provider);
        
        // Setup OutputTransform mock
        var outputTransform = new StructuredOutputTransform<EvidenceResult>(
            chatCompletionMock.Object, 
            new OpenAIPromptExecutionSettings());

        // Act
        var result = factory.TestCreateOrchestration(
            input,
            kernel,
            agents,
            outputTransform
        );

        // Assert
        Assert.NotNull(result);
        Assert.IsType<SequentialOrchestration<string, EvidenceResult>>(result);
    }

    #endregion

    // Testable Wrapper
    public class TestableEvidenceExtractionOrchestrationFactory : EvidenceExtractionOrchestrationFactory
    {
        public TestableEvidenceExtractionOrchestrationFactory(
            IAgentService agentService,
            IKernelBuilderService kernelBuilderService,
            IOptions<OrchestrationSettings> orchestrationSettings,
            ILoggerFactory loggerFactory,
            IAgentResponsePersistence? agentResponsePersistence = null)
            : base(agentService, kernelBuilderService, orchestrationSettings, loggerFactory, agentResponsePersistence)
        {
        }

        public AgentOrchestration<string, EvidenceResult> TestCreateOrchestration(
            OrchestrationPromptInput input,
            Kernel kernel,
            Agent[] agents,
            StructuredOutputTransform<EvidenceResult> outputTransform)
        {
            return base.CreateOrchestration(input, kernel, agents, outputTransform);
        }

        public string TestGetResultTypeName() => base.GetResultTypeName();

        public List<Evidence> TestUnwrapResult(EvidenceResult wrapper) => base.UnwrapResult(wrapper);

        public int TestGetItemCount(List<Evidence> result) => base.GetItemCount(result);

        public List<Evidence> TestCreateEmptyResult() => base.CreateEmptyResult();

        public List<Evidence> TestCreateErrorResult() => base.CreateErrorResult();

        public string TestGetAgentSelectionReason(string? previousAgentName) => base.GetAgentSelectionReason(previousAgentName);
    }
}
