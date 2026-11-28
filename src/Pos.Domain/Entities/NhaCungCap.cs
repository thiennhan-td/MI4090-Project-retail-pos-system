namespace Pos.Domain.Entities;

public sealed class NhaCungCap
{
    public NhaCungCap(
        string maNhaCungCap,
        string tenNhaCungCap,
        string? thongTinLienHe)
    {
        MaNhaCungCap = QuyTac.Chuoi(maNhaCungCap, 32);
        TenNhaCungCap = QuyTac.Chuoi(tenNhaCungCap, 150);
        QuyTac.Dat(thongTinLienHe is null || thongTinLienHe.Length <= 500, "Thông tin liên hệ không được dài quá 500 ký tự.");
        ThongTinLienHe = thongTinLienHe ?? string.Empty;
    }

    public string MaNhaCungCap { get; }
    public string TenNhaCungCap { get; }
    public string ThongTinLienHe { get; }
}
