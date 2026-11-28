namespace Pos.Domain.Entities;

public sealed class KhoanHoan
{
    public KhoanHoan(
        string maKhoanHoan,
        string maPhieuTra,
        string phuongThuc,
        decimal soTienHoan)
    {
        MaKhoanHoan = QuyTac.Chuoi(maKhoanHoan, 32);
        MaPhieuTra = QuyTac.Chuoi(maPhieuTra, 32);
        PhuongThuc = QuyTac.PhuongThuc(phuongThuc);
        SoTienHoan = QuyTac.TienDuong(soTienHoan);
    }

    public string MaKhoanHoan { get; }
    public string MaPhieuTra { get; }
    public string PhuongThuc { get; }
    public decimal SoTienHoan { get; }
}
