using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Abstractions;

public interface IIncidentStore
{
    Task SaveAsync(Incident incident, CancellationToken ct = default);
    Task UpdateAsync(Incident incident, CancellationToken ct = default);
    Task<IReadOnlyList<Incident>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Incident>> GetByDateRangeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task<Incident?> GetByIdAsync(string id, CancellationToken ct = default);
    Task RecoverOrphansAsync(TimeSpan maxActiveAge, CancellationToken ct = default);
}
