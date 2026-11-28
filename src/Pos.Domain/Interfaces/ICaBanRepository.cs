using Pos.Domain.Entities;

namespace Pos.Domain.Interfaces;

public interface ICaBanRepository
{
    Task<string> SinhMaAsync(CancellationToken ct);
    Task<IReadOnlyList<CaBan>> LayCaDangMoAsync(CancellationToken ct);
    Task<IReadOnlyList<CaBan>> LayTrongKyAsync(DateTime tuUtc, DateTime denUtc, CancellationToken ct);
    Task<SoQuyCa> TongHopKetAsync(string maCa, CancellationToken ct);
    Task<CaBan?> LayTheoMaAsync(string ma, CancellationToken ct);
    Task ThemAsync(CaBan entity, CancellationToken ct);
    Task CapNhatAsync(CaBan entity, CancellationToken ct);
    Task<CaBan?> LayCaMoAsync(string maNguoiMo, CancellationToken ct);
    Task<CaBan?> KhoaTheoMaAsync(string maCa, CancellationToken ct);
}
