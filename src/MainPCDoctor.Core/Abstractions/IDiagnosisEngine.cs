using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Abstractions;

public interface IDiagnosisEngine
{
    DiagnosisResult Evaluate(IReadOnlyList<SystemSnapshot> recentWindow);
}
