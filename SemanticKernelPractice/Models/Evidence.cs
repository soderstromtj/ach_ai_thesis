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
}
