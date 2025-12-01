using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SemanticKernelPractice.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EvidenceType
    {
        Fact,
        Assumption
    }
}
