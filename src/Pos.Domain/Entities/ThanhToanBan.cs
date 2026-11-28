using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class ThanhToanBan
{
    public ThanhToanBan(
        string maThanhToan,
        string maHoaDon,
        string phuongThuc,
        decimal soTienThanhToan,
        decimal? tienKhachDua)
    {
        MaThanhToan = QuyTac.Chuoi(maThanhToan, 32);
        MaHoaDon = QuyTac.Chuoi(maHoaDon, 32);
        PhuongThuc = QuyTac.PhuongThuc(phuongThuc);
        SoTienThanhToan = QuyTac.TienDuong(soTienThanhToan);
        TienKhachDua = tienKhachDua is null ? null : QuyTac.Tien(tienKhachDua.Value);
        QuyTac.Dat(PhuongThuc == PhuongThucThanhToan.TienMat
            ? TienKhachDua >= SoTienThanhToan : TienKhachDua is null, "Tiền khách đưa không hợp lệ.");
    }

    public string MaThanhToan { get; }
    public string MaHoaDon { get; }
    public string PhuongThuc { get; }
    public decimal SoTienThanhToan { get; }
    public decimal? TienKhachDua { get; }

    public decimal TienThua => TienKhachDua.GetValueOrDefault(SoTienThanhToan) - SoTienThanhToan;
}
