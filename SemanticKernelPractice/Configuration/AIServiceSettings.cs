using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPractice.Configuration
{
    /// <summary>
    /// Global AI service provider configurations.
    /// The Provider selection is now per-experiment in ExperimentAIServiceSettings.
    /// </summary>
    public class AIServiceSettings
    {
        public AzureOpenAISettings? AzureOpenAI { get; set; }
        public OpenAISettings? OpenAI { get; set; }
        public OllamaSettings? Ollama { get; set; }
        public HuggingFaceSettings? HuggingFace { get; set; }
    }
}
