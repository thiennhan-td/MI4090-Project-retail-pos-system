using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IPhieuThuChiRepository
{
    Task<string> SinhMaAsync(CancellationToken ct);
    Task<PhieuThuChi?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(PhieuThuChi entity, CancellationToken ct);
}
