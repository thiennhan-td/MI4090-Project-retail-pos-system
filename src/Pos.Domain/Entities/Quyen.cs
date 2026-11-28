namespace Pos.Domain.Entities;

public sealed class Quyen
{
    public Quyen(
        string maQuyen,
        string tenQuyen)
    {
        MaQuyen = QuyTac.Chuoi(maQuyen, 64);
        TenQuyen = QuyTac.Chuoi(tenQuyen, 150);
    }

    public string MaQuyen { get; }
    public string TenQuyen { get; }
}
