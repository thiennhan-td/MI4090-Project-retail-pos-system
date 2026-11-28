using Pos.Domain.Entities;
using Pos.Domain.Exceptions;

namespace Pos.Counter.Services;

public static class PhienBanNhap
{
    // Repository cấp lại mã dòng khi thay nội dung nháp. Sắp xếp để thứ tự đọc SQL không ảnh hưởng phiên bản.
    public static string CuaHoaDon(HoaDon h) => CounterRequest.Bam(new
    {
        h.MaHoaDon,
        h.TrangThai,
        DongBan = h.DongBans.OrderBy(d => d.MaDongBan, StringComparer.Ordinal)
            .Select(d => new { d.MaDongBan, d.MaHang, d.SoLuongBan, Gia = CounterRequest.ChuanHoaTien(d.DonGiaThucBan) }),
        ThanhToan = h.ThanhToanBans.OrderBy(t => t.MaThanhToan, StringComparer.Ordinal)
            .Select(t => new { t.MaThanhToan, t.PhuongThuc, Tien = CounterRequest.ChuanHoaTien(t.SoTienThanhToan),
                KhachDua = CounterRequest.ChuanHoaTien(t.TienKhachDua) })
    });

    public static string CuaPhieuTra(PhieuTra p) => CounterRequest.Bam(new
    {
        p.MaPhieuTra,
        p.TrangThai,
        DongTra = p.DongTras.OrderBy(d => d.MaDongTra, StringComparer.Ordinal)
            .Select(d => new { d.MaDongTra, d.MaDongBan, d.SoLuongTra, d.SoLuongNhapLai }),
        KhoanHoan = p.KhoanHoans.OrderBy(k => k.MaKhoanHoan, StringComparer.Ordinal)
            .Select(k => new { k.MaKhoanHoan, k.PhuongThuc, Tien = CounterRequest.ChuanHoaTien(k.SoTienHoan) })
    });

    internal static void KiemTra(string hienTai, string? daXem)
    {
        if (!string.Equals(hienTai, daXem, StringComparison.Ordinal))
        {
            throw new ConcurrencyConflictException("Bản nháp đã thay đổi. Hãy tải lại chứng từ trước khi tiếp tục.");
        }
    }
}
