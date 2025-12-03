namespace SemanticKernelPractice.Models
{
    public class Evidence
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public EvidenceType Type { get; set; }
    }
}
