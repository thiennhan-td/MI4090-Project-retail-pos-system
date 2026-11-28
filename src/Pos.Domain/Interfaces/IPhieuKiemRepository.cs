using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IPhieuKiemRepository
{
    Task<string> SinhMaAsync(CancellationToken ct);
    Task<PhieuKiem?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task<PhieuKiem?> KhoaTheoMaAsync(string ma, CancellationToken ct);
    Task<IReadOnlyList<PhieuKiem>> LayLichSuAsync(CancellationToken ct);
    Task<DateTime?> LayMocKiemDangMoMoiNhatAsync(string maHang, CancellationToken ct);
    Task ThemAsync(PhieuKiem entity, CancellationToken ct);
    Task CapNhatAsync(PhieuKiem entity, CancellationToken ct);
    Task CapNhatDemAsync(DongKiem dong, CancellationToken ct);
}
