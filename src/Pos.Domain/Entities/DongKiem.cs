namespace Pos.Domain.Entities;

public sealed class DongKiem
{
    public DongKiem(
        string maPhieuKiem,
        string maHang,
        int tonSoTaiMoc,
        int? soLuongDem,
        string? giaiTrinh)
    {
        MaPhieuKiem = QuyTac.Chuoi(maPhieuKiem, 32);
        MaHang = QuyTac.Chuoi(maHang, 32);
        TonSoTaiMoc = QuyTac.Luong(tonSoTaiMoc);
        SoLuongDem = soLuongDem is null ? null : QuyTac.Luong(soLuongDem.Value);
        GiaiTrinh = giaiTrinh is null ? null : QuyTac.Chuoi(giaiTrinh, 500);
    }

    public string MaPhieuKiem { get; }
    public string MaHang { get; }
    public int TonSoTaiMoc { get; }
    public int? SoLuongDem { get; }
    public string? GiaiTrinh { get; }

    public int? ChenhLech => SoLuongDem - TonSoTaiMoc;
}
