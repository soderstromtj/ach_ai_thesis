using System.Text.Json.Serialization;

namespace NIU.ACH_AI.Domain.ValueObjects
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EvaluationScore
    {
        VeryConsistent = 2,
        Consistent = 1,
        Neutral = 0,
        Inconsistent = -1,
        VeryInconsistent = -2
    }
}
