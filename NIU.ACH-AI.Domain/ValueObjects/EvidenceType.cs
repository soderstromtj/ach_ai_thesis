using System.Text.Json.Serialization;

namespace NIU.ACH_AI.Domain.ValueObjects
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EvidenceType
    {
        Fact = 1,
        Assumption = 2,
        ExpertOpinion = 3
    }
}
