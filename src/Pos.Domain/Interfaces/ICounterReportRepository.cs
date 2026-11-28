namespace Pos.Domain.Interfaces;

public sealed record SoQuyCa(decimal ThuBan, decimal HoanTra, decimal ThuRieng, decimal ChiRieng, decimal DaoHuy);

public sealed record SuKienBanTra(DateTime LucUtc, string MaChungTu, string MaCa, string Loai,
    string? MaHang, int SoLuong, decimal SoTien, string? PhuongThuc);

public interface ICounterReportRepository
{
    Task<IReadOnlyList<SuKienBanTra>> LaySuKienAsync(DateTime tuUtc, DateTime denUtc, CancellationToken ct);
}
