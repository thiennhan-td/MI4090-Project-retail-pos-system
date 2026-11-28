using Pos.Domain.Constants;
using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface IPhieuNhapRepository
{
    Task<string> SinhMaPhieuNhapAsync(CancellationToken ct);
    Task<string> SinhMaDongNhapAsync(CancellationToken ct);
    /// <summary>Đọc nhất quán đầu phiếu, chi tiết và khoản trả trong giao dịch hiện hành.</summary>
    Task<PhieuNhap?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(PhieuNhap entity, CancellationToken ct);
    Task CapNhatNhapAsync(PhieuNhap entity, IReadOnlyList<DongNhap> dongCu, CancellationToken ct);
    Task CapNhatTrangThaiAsync(PhieuNhap entity, string trangThaiTruoc, CancellationToken ct);
    Task<PhieuNhap?> KhoaTheoMaAsync(string maPhieuNhap, CancellationToken ct);
    Task<TraNhaCungCap?> LayKhoanTraTheoMaAsync(string maKhoanTra, CancellationToken ct);
    Task ThemKhoanTraAsync(TraNhaCungCap khoanTra, CancellationToken ct);
    Task<IReadOnlyList<TomTatPhieuNhap>> TraCuuAsync(string? maNhaCungCap, string? trangThai,
        DateTime? tuUtc, DateTime? denUtc, bool xemCongNo, CancellationToken ct);
}

public sealed record TomTatPhieuNhap(string MaPhieuNhap, string MaNhaCungCap, string MaNguoiLap,
    DateTime LucLap, DateOnly? HanThanhToan, string TrangThai, decimal TongTien, decimal? DaTra)
{
    public decimal? DuNo => DaTra is null ? null : TrangThai == TrangThaiChungTu.DaXacNhan
        ? TongTien - DaTra : 0;
}
