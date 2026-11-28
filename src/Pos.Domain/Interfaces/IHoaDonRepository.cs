using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IHoaDonRepository
{
    Task<string> SinhMaAsync(CancellationToken ct);
    Task<string> SinhMaDongAsync(CancellationToken ct);
    Task<string> SinhMaThanhToanAsync(CancellationToken ct);
    Task<HoaDon?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(HoaDon entity, CancellationToken ct);
    Task CapNhatAsync(HoaDon entity, CancellationToken ct);
    Task<HoaDon?> KhoaTheoMaAsync(string maHoaDon, CancellationToken ct);
}
