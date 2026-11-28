using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IQuanTriTaiKhoanRepository : ITaiKhoanRepository
{
    Task<HoSoDangNhap?> LayHoSoDangNhapAsync(string maTaiKhoan, CancellationToken ct);
    Task<IReadOnlyList<TaiKhoan>> TimTaiKhoanAsync(string? tuKhoa, CancellationToken ct);
    Task<string> SinhMaTaiKhoanAsync(CancellationToken ct);
    Task<VaiTro?> KhoaVaiTroAsync(string maVaiTro, CancellationToken ct);
    Task<int> DemQuanLyKhacDangHoatDongAsync(string maTaiKhoan, CancellationToken ct);
    Task<IReadOnlyList<VaiTro>> LayVaiTroAsync(CancellationToken ct);
    Task<IReadOnlyList<Quyen>> LayDanhSachQuyenAsync(CancellationToken ct);
    Task<IReadOnlySet<string>> LayQuyenVaiTroAsync(string maVaiTro, CancellationToken ct);
    Task ThayQuyenVaiTroAsync(string maVaiTro, IReadOnlySet<string> maQuyen, CancellationToken ct);
}

public sealed record HoSoDangNhap(TaiKhoan TaiKhoan, IReadOnlySet<string> Quyen);
