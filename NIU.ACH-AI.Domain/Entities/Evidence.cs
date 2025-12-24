using NIU.ACH_AI.Domain.ValueObjects;

namespace NIU.ACH_AI.Domain.Entities
{
    public class Evidence
    {
        public Guid EvidenceId { get; set; }
        public string Claim { get; set; } = string.Empty;
        public string? ReferenceSnippet { get; set; }
        public EvidenceType Type { get; set; }
        public string Notes { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"EvidenceId: {EvidenceId}\n" +
                   $"Claim: {Claim}\n" +
                   $"Type: {Type}\n" +
                   $"Notes: {Notes ?? "N/A"}\n" +
                   $"ReferenceSnippet: {ReferenceSnippet ?? "N/A"}";
        }
    }
}
