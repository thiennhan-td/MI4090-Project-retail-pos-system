using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IPhieuKiemRepository
{
    Task<PhieuKiem?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(PhieuKiem entity, CancellationToken ct);
    Task CapNhatAsync(PhieuKiem entity, CancellationToken ct);
}
