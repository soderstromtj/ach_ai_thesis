using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

/// <summary>
/// Represents a category or type of evidence.
/// Maps to the EVIDENCE_TYPES table.
/// </summary>
public partial class EvidenceType
{
    /// <summary>
    /// Gets or sets the unique identifier for the evidence type.
    /// </summary>
    public int EvidenceTypeId { get; set; }

    /// <summary>
    /// Gets or sets the name of the evidence type (e.g., "Report", "Observation").
    /// </summary>
    public string EvidenceTypeName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the description of the evidence type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the collection of evidence items of this type.
    /// </summary>
    public virtual ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();
}
