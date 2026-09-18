namespace MainPCDoctor.Core.Models;

public record UpgradeRecommendation(
    string         Id,
    string         Component,
    ConfidenceLevel Confidence,
    float          EvidenceScore,
    int            ObservationDays,
    string         EvidenceJson,
    DateTimeOffset GeneratedAt,
    DateTimeOffset? NotificationSentAt
);
