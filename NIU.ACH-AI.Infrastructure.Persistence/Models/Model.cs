using System;
using System.Collections.Generic;

namespace NIU.ACH_AI.Infrastructure.Persistence.Models;

public partial class Model
{
    public Guid ModelId { get; set; }

    public Guid ProviderId { get; set; }

    public string ModelName { get; set; } = null!;

    public decimal? InputTokenCost { get; set; }

    public decimal? CachedInputTokenCost { get; set; }

    public decimal? OutputTokenCost { get; set; }

    public virtual ICollection<AgentConfiguration> AgentConfigurations { get; set; } = new List<AgentConfiguration>();

    public virtual Provider Provider { get; set; } = null!;
}
