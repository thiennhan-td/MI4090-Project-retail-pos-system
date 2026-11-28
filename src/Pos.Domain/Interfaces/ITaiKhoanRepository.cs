using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface ITaiKhoanRepository
{
    Task<TaiKhoan?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(TaiKhoan entity, CancellationToken ct);
    Task CapNhatAsync(TaiKhoan entity, CancellationToken ct);
    Task<TaiKhoan?> LayTheoTenDangNhapAsync(string tenDangNhap, CancellationToken ct);
    Task<IReadOnlySet<string>> LayTapQuyenAsync(string maTaiKhoan, CancellationToken ct);
}
