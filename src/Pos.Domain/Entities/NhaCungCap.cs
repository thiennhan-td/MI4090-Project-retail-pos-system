namespace Pos.Domain.Entities;

public sealed class NhaCungCap
{
    public NhaCungCap(
        string maNhaCungCap,
        string tenNhaCungCap,
        string thongTinLienHe)
    {
        MaNhaCungCap = QuyTac.Chuoi(maNhaCungCap, 32);
        TenNhaCungCap = QuyTac.Chuoi(tenNhaCungCap, 150);
        ThongTinLienHe = QuyTac.Chuoi(thongTinLienHe, 500);
    }

    public string MaNhaCungCap { get; }
    public string TenNhaCungCap { get; }
    public string ThongTinLienHe { get; }
}
