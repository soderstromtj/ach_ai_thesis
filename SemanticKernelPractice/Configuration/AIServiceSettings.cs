using Azure.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPractice.Configuration
{
    public class AIServiceSettings
    {
        public AIServiceProvider Provider { get; set; } = AIServiceProvider.AzureOpenAI;
        public AzureOpenAISettings? AzureOpenAI { get; set; }
        public OpenAISettings? OpenAI { get; set; }
        public OllamaSettings? Ollama { get; set; }
        public HuggingFaceSettings? HuggingFace { get; set; }
    }
}
