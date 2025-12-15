
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.DTOs
{
    public class HypothesisResult
    {
        public List<Hypothesis> Hypotheses { get; set; } = new List<Hypothesis>();
    }
}
