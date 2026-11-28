namespace Pos.Domain.Entities;

public sealed class DongKiem
{
    public DongKiem(
        string maPhieuKiem,
        string maHang,
        int tonSoTaiMoc,
        int? soLuongDem,
        string? giaiTrinh,
        DateTime? lucDem = null)
    {
        MaPhieuKiem = QuyTac.Chuoi(maPhieuKiem, 32);
        MaHang = QuyTac.Chuoi(maHang, 32);
        TonSoTaiMoc = QuyTac.Luong(tonSoTaiMoc);
        SoLuongDem = soLuongDem is null ? null : QuyTac.Luong(soLuongDem.Value);
        GiaiTrinh = giaiTrinh is null ? null : QuyTac.Chuoi(giaiTrinh, 500);
        QuyTac.Dat(lucDem is null || soLuongDem.HasValue, "Chỉ dòng đã đếm mới có thời điểm đếm.");
        LucDem = lucDem.HasValue ? QuyTac.Utc(lucDem.Value) : null;
    }

    public string MaPhieuKiem { get; }
    public string MaHang { get; }
    public int TonSoTaiMoc { get; }
    public int? SoLuongDem { get; }
    public string? GiaiTrinh { get; }
    public DateTime? LucDem { get; }

    public int? ChenhLech => SoLuongDem - TonSoTaiMoc;
}
