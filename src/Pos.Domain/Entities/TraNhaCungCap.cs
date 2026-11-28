using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class TraNhaCungCap
{
    public TraNhaCungCap(
        string maKhoanTra,
        string maPhieuNhap,
        string maNguoiGhi,
        string? maCaChi,
        DateTime lucTra,
        decimal soTienTra,
        string phuongThuc,
        string nguonChi)
    {
        MaKhoanTra = QuyTac.Chuoi(maKhoanTra, 32);
        MaPhieuNhap = QuyTac.Chuoi(maPhieuNhap, 32);
        MaNguoiGhi = QuyTac.Chuoi(maNguoiGhi, 32);
        MaCaChi = maCaChi is null ? null : QuyTac.Chuoi(maCaChi, 32);
        LucTra = QuyTac.Utc(lucTra);
        SoTienTra = QuyTac.TienDuong(soTienTra);
        PhuongThuc = QuyTac.PhuongThuc(phuongThuc);
        NguonChi = QuyTac.Chuoi(nguonChi, 24);
        QuyTac.Dat(
            (NguonChi == NguonQuy.KetCa && PhuongThuc == PhuongThucThanhToan.TienMat && MaCaChi is not null)
            || (NguonChi == NguonQuy.QuyCuaHang && MaCaChi is null),
            "Nguồn chi và ca chi không phù hợp.");
    }

    public string MaKhoanTra { get; }
    public string MaPhieuNhap { get; }
    public string MaNguoiGhi { get; }
    public string? MaCaChi { get; }
    public DateTime LucTra { get; }
    public decimal SoTienTra { get; }
    public string PhuongThuc { get; }
    public string NguonChi { get; }
}
