using System.Text;
using NIU.ACH_AI.Application.DTOs;

namespace NIU.ACH_AI.Application.Services
{
    public class OrchestrationPromptFormatter : Interfaces.IOrchestrationPromptFormatter
    {
        public string FormatPrompt(OrchestrationPromptInput input)
        {
            if (input == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Key Question: {input.KeyQuestion}");
            sb.AppendLine($"Context: {input.Context}");
            sb.AppendLine($"Task Instructions: {input.TaskInstructions}");

            if (input.HypothesisResult != null)
            {
                sb.AppendLine($"Hypotheses: {System.Text.Json.JsonSerializer.Serialize(input.HypothesisResult)}");
            }
            
            if (input.EvidenceResult != null)
            {
                sb.AppendLine($"Evidence: {System.Text.Json.JsonSerializer.Serialize(input.EvidenceResult)}");
            }
            
            if (!string.IsNullOrWhiteSpace(input.AdditionalInstructions))
            {
                sb.AppendLine($"Additional Instructions: {input.AdditionalInstructions}");
            }
                
            return sb.ToString();
        }
    }
}
