using System.Text.Json.Serialization;

namespace NIU.ACH_AI.Domain.ValueObjects
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EvaluationScore
    {
        VeryConsistent,
        Consistent,
        Neutral,
        Inconsistent,
        VeryInconsistent
    }
}
