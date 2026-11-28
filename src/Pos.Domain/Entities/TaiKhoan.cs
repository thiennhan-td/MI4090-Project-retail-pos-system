using Pos.Domain.Constants;

namespace Pos.Domain.Entities;

public sealed class TaiKhoan
{
    public TaiKhoan(
        string maTaiKhoan,
        string maVaiTro,
        string hoTen,
        string tenDangNhap,
        string matKhauBam)
    {
        MaTaiKhoan = QuyTac.Chuoi(maTaiKhoan, 32);
        MaVaiTro = QuyTac.Chuoi(maVaiTro, 32);
        HoTen = QuyTac.Chuoi(hoTen, 150);
        TenDangNhap = QuyTac.Chuoi(tenDangNhap, 64);
        MatKhauBam = QuyTac.Chuoi(matKhauBam, 256);
        TrangThai = TrangThaiChungTu.HoatDong;
    }

    public string MaTaiKhoan { get; }
    public string MaVaiTro { get; private set; }
    public string HoTen { get; }
    public string TenDangNhap { get; }
    public string MatKhauBam { get; private set; }
    public string TrangThai { get; private set; }

    public void DoiVaiTro(string maVaiTro) => MaVaiTro = QuyTac.Chuoi(maVaiTro, 32);
    public void DoiMatKhauBam(string matKhauBam) => MatKhauBam = QuyTac.Chuoi(matKhauBam, 256);
    public void Khoa() => TrangThai = TrangThaiChungTu.BiKhoa;
    public void MoKhoa() => TrangThai = TrangThaiChungTu.HoatDong;
}
