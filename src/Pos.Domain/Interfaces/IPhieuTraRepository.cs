using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IPhieuTraRepository
{
    Task<PhieuTra?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(PhieuTra entity, CancellationToken ct);
    Task CapNhatAsync(PhieuTra entity, CancellationToken ct);
    Task XoaNhapAsync(string maPhieuTra, CancellationToken ct);
    Task<int> TinhLuongDaTraAsync(string maDongBan, CancellationToken ct);
}
