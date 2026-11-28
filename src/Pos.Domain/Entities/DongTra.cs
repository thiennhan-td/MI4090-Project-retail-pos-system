namespace Pos.Domain.Entities;

public sealed class DongTra
{
    public DongTra(
        string maDongTra,
        string maPhieuTra,
        string maDongBan,
        int soLuongTra,
        int soLuongNhapLai)
    {
        MaDongTra = QuyTac.Chuoi(maDongTra, 32);
        MaPhieuTra = QuyTac.Chuoi(maPhieuTra, 32);
        MaDongBan = QuyTac.Chuoi(maDongBan, 32);
        SoLuongTra = QuyTac.LuongDuong(soLuongTra);
        SoLuongNhapLai = QuyTac.Luong(soLuongNhapLai);
        QuyTac.Dat(SoLuongNhapLai <= SoLuongTra, "Số lượng nhập lại vượt số lượng trả.");
    }

    public string MaDongTra { get; }
    public string MaPhieuTra { get; }
    public string MaDongBan { get; }
    public int SoLuongTra { get; }
    public int SoLuongNhapLai { get; }
}
