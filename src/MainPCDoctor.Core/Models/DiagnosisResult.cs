namespace MainPCDoctor.Core.Models;

public enum DiagnosisOutcome { Clean, Candidate, Confirmed, InsufficientEvidence }

public record RuleFinding(
    BottleneckType  BottleneckType,
    DiagnosisOutcome Outcome,
    int             NotificationLevel,
    string?         ProcessName = null,
    string?         Note        = null
);

public record DiagnosisResult(
    DateTimeOffset        EvaluatedAt,
    RuleFinding?          Primary,
    IReadOnlyList<RuleFinding> Secondary
)
{
    public static DiagnosisResult Clean(DateTimeOffset at) =>
        new(at, null, Array.Empty<RuleFinding>());
}
