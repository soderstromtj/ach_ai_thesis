using System.Text;

namespace NIU.ACH_AI.Application.DTOs
{
    public class OrchestrationPromptInput
    {
        public string KeyQuestion { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public string TaskInstructions { get; set; } = string.Empty;
        public HypothesisResult? HypothesisResult { get; set; } = null;
        public EvidenceResult? EvidenceResult { get; set; } = null;
        public string AdditionalInstructions { get; set; } = string.Empty;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Key Question: {KeyQuestion}");
            sb.AppendLine($"Context: {Context}");
            sb.AppendLine($"Task Instructions: {TaskInstructions}");

            if (HypothesisResult != null)
            {
                sb.AppendLine($"Hypotheses: {System.Text.Json.JsonSerializer.Serialize(HypothesisResult)}");
            }
            
            if (EvidenceResult != null)
            {
                sb.AppendLine($"Evidence: {System.Text.Json.JsonSerializer.Serialize(EvidenceResult)}");
            }
            
            if (!string.IsNullOrWhiteSpace(AdditionalInstructions))
            {
                sb.AppendLine($"Additional Instructions: {AdditionalInstructions}");
            }
                
            return sb.ToString();
        }
    }
}
