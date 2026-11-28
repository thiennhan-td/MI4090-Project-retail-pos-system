using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IHoaDonRepository
{
    Task<HoaDon?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(HoaDon entity, CancellationToken ct);
    Task CapNhatAsync(HoaDon entity, CancellationToken ct);
    Task<HoaDon?> KhoaTheoMaAsync(string maHoaDon, CancellationToken ct);
}
