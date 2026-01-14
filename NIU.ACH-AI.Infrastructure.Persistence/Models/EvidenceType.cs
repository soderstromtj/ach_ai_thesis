using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class EvidenceType
{
    public int EvidenceTypeId { get; set; }

    public string EvidenceTypeName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();
}
