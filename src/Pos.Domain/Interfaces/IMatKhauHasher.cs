namespace Pos.Domain.Interfaces;

public interface IMatKhauHasher
{
    string Bam(string matKhau);
    bool KiemTra(string matKhau, string matKhauBam);
}
