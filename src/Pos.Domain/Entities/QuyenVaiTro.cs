namespace Pos.Domain.Entities;

public sealed class QuyenVaiTro
{
    public QuyenVaiTro(
        string maVaiTro,
        string maQuyen)
    {
        MaVaiTro = QuyTac.Chuoi(maVaiTro, 32);
        MaQuyen = QuyTac.Chuoi(maQuyen, 64);
    }

    public string MaVaiTro { get; }
    public string MaQuyen { get; }
}
