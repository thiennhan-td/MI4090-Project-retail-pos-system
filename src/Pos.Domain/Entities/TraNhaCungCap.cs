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
        KiemTraKhoanTra(soTienTra, phuongThuc, nguonChi, maCaChi);
        MaCaChi = maCaChi;
        LucTra = QuyTac.Utc(lucTra);
        SoTienTra = soTienTra;
        PhuongThuc = phuongThuc;
        NguonChi = nguonChi;
    }

    public static void KiemTraKhoanTra(decimal soTienTra, string phuongThuc, string nguonChi, string? maCaChi)
    {
        QuyTac.TienDuong(soTienTra);
        QuyTac.PhuongThuc(phuongThuc);
        QuyTac.Dat(
            (nguonChi == NguonQuy.KetCa && phuongThuc == PhuongThucThanhToan.TienMat && maCaChi is not null)
            || (nguonChi == NguonQuy.QuyCuaHang && maCaChi is null),
            "Nguồn chi và ca chi không phù hợp.");
        if (maCaChi is not null)
        {
            QuyTac.Chuoi(maCaChi, 32);
        }
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
