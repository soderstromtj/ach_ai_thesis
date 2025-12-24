using NIU.ACH_AI.Application.Configuration;
using NIU.ACH_AI.Domain.Entities;

namespace NIU.ACH_AI.Application.Interfaces
{
    public interface IResultPresenter
    {
        void DisplayExperimentInfo(ExperimentConfiguration experimentConfiguration);
        void DisplayHypotheses(string title, IEnumerable<Hypothesis> hypotheses);
        void DisplayEvidence(string title, IEnumerable<Evidence> evidence);
        void DisplayEvaluation(EvidenceHypothesisEvaluation evaluation);
        void DisplayErrorMessage(string message);
    }
}
