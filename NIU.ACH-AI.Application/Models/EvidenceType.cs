using System.Text.Json.Serialization;

namespace NIU.ACHAI.Application.Models
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
