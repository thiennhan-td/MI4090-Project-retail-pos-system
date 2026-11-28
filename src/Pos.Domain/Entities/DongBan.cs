namespace Pos.Domain.Entities;

public sealed class DongBan
{
    public DongBan(
        string maDongBan,
        string maHoaDon,
        string maHang,
        int soLuongBan,
        decimal donGiaThucBan)
    {
        MaDongBan = QuyTac.Chuoi(maDongBan, 32);
        MaHoaDon = QuyTac.Chuoi(maHoaDon, 32);
        MaHang = QuyTac.Chuoi(maHang, 32);
        SoLuongBan = QuyTac.LuongDuong(soLuongBan);
        DonGiaThucBan = QuyTac.Tien(donGiaThucBan);
    }

    public string MaDongBan { get; }
    public string MaHoaDon { get; }
    public string MaHang { get; }
    public int SoLuongBan { get; }
    public decimal DonGiaThucBan { get; }

    public decimal ThanhTien => SoLuongBan * DonGiaThucBan;
}
