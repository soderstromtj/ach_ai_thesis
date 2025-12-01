using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPractice.Models
{
    public class Evidence
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public EvidenceType Type { get; set; }
    }

    /// <summary>
    /// Wrapper class for structured output - OpenAI requires top-level object, not array
    /// </summary>
    public class EvidenceResult
    {
        public List<Evidence> Evidence { get; set; } = new List<Evidence>();
    }
}
