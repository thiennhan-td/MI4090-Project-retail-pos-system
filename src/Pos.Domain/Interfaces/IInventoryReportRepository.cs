using Pos.Domain.Reports;

namespace Pos.Domain.Interfaces;

public interface IInventoryReportRepository
{
    // Các khoảng báo cáo dùng UTC, bao gồm đầu kỳ và loại trừ cuối kỳ.
    Task<IReadOnlyList<DongXuatNhapTonDto>> LayXuatNhapTonAsync(
        DateTime tuUtc, DateTime denUtc, string? maNhom, string? maHang, CancellationToken ct);

    Task<IReadOnlyList<DongNhapCongNoDto>> LayNhapCongNoAsync(
        DateTime tuUtc, DateTime denUtc, string? maNhaCungCap, CancellationToken ct);

    Task<decimal> TinhTongChiNccTuKetTheoCaAsync(string maCa, CancellationToken ct);
    Task<ChiNccTheoKyDto> LayTongHopChiNccTheoKyAsync(DateTime tuUtc, DateTime denUtc, CancellationToken ct);
}
