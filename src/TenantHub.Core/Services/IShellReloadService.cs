namespace TenantHub.Core.Services;

public interface IShellReloadService
{
    Task AddShellAsync(string slug, IReadOnlyList<string> features, CancellationToken ct = default);
    Task UpdateShellAsync(string slug, IReadOnlyList<string> features, CancellationToken ct = default);
    Task RemoveShellAsync(string slug, CancellationToken ct = default);
}
