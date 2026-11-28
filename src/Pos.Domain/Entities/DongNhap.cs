namespace Pos.Domain.Entities;

public sealed class DongNhap
{
    public DongNhap(
        string maDongNhap,
        string maPhieuNhap,
        string maHang,
        int soLuongNhap,
        decimal donGiaNhap)
    {
        MaDongNhap = QuyTac.Chuoi(maDongNhap, 32);
        MaPhieuNhap = QuyTac.Chuoi(maPhieuNhap, 32);
        MaHang = QuyTac.Chuoi(maHang, 32);
        SoLuongNhap = QuyTac.LuongDuong(soLuongNhap);
        DonGiaNhap = QuyTac.Tien(donGiaNhap);
    }

    public string MaDongNhap { get; }
    public string MaPhieuNhap { get; }
    public string MaHang { get; }
    public int SoLuongNhap { get; }
    public decimal DonGiaNhap { get; }

    public decimal ThanhTien => SoLuongNhap * DonGiaNhap;
}
