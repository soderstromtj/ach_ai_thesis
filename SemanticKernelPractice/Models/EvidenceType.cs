using System.Text.Json.Serialization;

namespace SemanticKernelPractice.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EvidenceType
    {
        DirectQuote,
        Paraphrase,
        Derived,
        Assumption
    }
}
