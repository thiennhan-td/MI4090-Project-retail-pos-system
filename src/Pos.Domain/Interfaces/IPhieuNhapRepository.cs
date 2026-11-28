using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IPhieuNhapRepository
{
    Task<PhieuNhap?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(PhieuNhap entity, CancellationToken ct);
    Task CapNhatAsync(PhieuNhap entity, CancellationToken ct);
    Task<PhieuNhap?> KhoaTheoMaAsync(string maPhieuNhap, CancellationToken ct);
}
