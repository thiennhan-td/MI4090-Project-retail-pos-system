using Pos.Contracts;
using Pos.Domain.Interfaces;

namespace Pos.Inventory.Services;

public sealed class InventoryReportQueryService(IInventoryReportRepository repository) : IInventoryReportQueryService
{
    public Task<decimal> TinhTongChiNccTuKetTheoCaAsync(string maCa, CancellationToken ct)
        => repository.TinhTongChiNccTuKetTheoCaAsync(maCa, ct);

    public async Task<TongHopChiNccTrongKyDto> LayTongHopChiNccTheoKyAsync(
        DateTime tuUtc, DateTime denUtc, CancellationToken ct)
    {
        var tongHop = await repository.LayTongHopChiNccTheoKyAsync(tuUtc, denUtc, ct);
        return new(tongHop.ChiTuKetCa, tongHop.ChiQuyCuaHangTienMat, tongHop.ChiQuyCuaHangChuyenKhoan);
    }
}
