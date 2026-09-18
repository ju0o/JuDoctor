using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Abstractions;

public interface ISystemMetricsCollector
{
    Task<SystemSnapshot> CollectAsync(CancellationToken cancellationToken = default);
    Task<SystemSnapshot> CollectMinimumAsync(CancellationToken cancellationToken = default);
    void Initialize();
    void Dispose();
}
