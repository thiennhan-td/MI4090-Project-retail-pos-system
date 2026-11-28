namespace Pos.Contracts;

public interface IInventoryReportQueryService
{
    Task<decimal> TinhTongChiNccTuKetTheoCaAsync(string maCa, CancellationToken ct);
    // Intervals are UTC, inclusive start and exclusive end.
    Task<TongHopChiNccTrongKyDto> LayTongHopChiNccTheoKyAsync(DateTime tuUtc, DateTime denUtc, CancellationToken ct);
}
