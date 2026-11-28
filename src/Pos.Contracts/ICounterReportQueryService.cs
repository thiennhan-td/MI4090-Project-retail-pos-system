namespace Pos.Contracts;

public interface ICounterReportQueryService
{
    // Intervals are UTC, inclusive start and exclusive end.
    Task<TongHopBanTraCaDto> LayTongHopBanTraTrongNgayAsync(DateTime tuUtc, DateTime denUtc, CancellationToken ct);
}
