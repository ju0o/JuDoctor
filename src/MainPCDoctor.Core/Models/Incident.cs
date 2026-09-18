namespace MainPCDoctor.Core.Models;

public enum IncidentStatus { Active, Resolved }

public class Incident
{
    public string         Id                { get; init; } = Guid.NewGuid().ToString();
    public BottleneckType BottleneckType    { get; init; }
    public IncidentStatus Status            { get; set;  }
    public DateTimeOffset StartedAt         { get; init; }
    public DateTimeOffset? ConfirmedAt      { get; set;  }
    public DateTimeOffset? ResolvedAt       { get; set;  }
    public string?        ResolveReason     { get; set;  }
    public float?         PeakCpuPercent    { get; set;  }
    public float?         PeakMemUsedGb     { get; set;  }
    public float?         PeakDiskLatencyMs { get; set;  }
    public float?         PeakGpuUtilPct    { get; set;  }
    public int?           DurationSeconds   { get; set;  }
    public int            NotificationLevel { get; init; }
    public DateTimeOffset? NotifiedAt       { get; set;  }
    public string?        EvidenceJson      { get; set;  }
}
