using NIU.ACH_AI.Domain.ValueObjects;

namespace NIU.ACH_AI.Domain.Entities
{
    public class Evidence
    {
        public Guid EvidenceId { get; set; }
        public string Claim { get; set; } = string.Empty;
        public string? Snippet { get; set; }
        public EvidenceType Type { get; set; }
        public double Confidence { get; set; }
        public string WhyConfident { get; set; } = string.Empty;
        public List<string> Constraints { get; set; } = new();
        public string Notes { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"EvidenceId: {EvidenceId}, Claim: {Claim}, Type: {Type}, Confidence: {Confidence}, WhyConfident: {WhyConfident}, Constraints: [{string.Join(", ", Constraints)}], Notes: {Notes}";
        }
    }
}
