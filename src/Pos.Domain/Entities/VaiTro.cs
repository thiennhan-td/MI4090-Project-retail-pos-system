namespace Pos.Domain.Entities;

public sealed class VaiTro
{
    public VaiTro(
        string maVaiTro,
        string tenVaiTro)
    {
        MaVaiTro = QuyTac.Chuoi(maVaiTro, 32);
        TenVaiTro = QuyTac.Chuoi(tenVaiTro, 150);
    }

    public string MaVaiTro { get; }
    public string TenVaiTro { get; }
}
