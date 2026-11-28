using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Interfaces;

namespace Pos.Counter.Services;

public sealed class CounterReportQueryService(ICounterReportRepository reports, IUnitOfWork work)
    : ICounterReportQueryService
{
    public Task<TongHopBanTraCaDto> LayTongHopBanTraTrongNgayAsync(DateTime tuUtc, DateTime denUtc, CancellationToken ct)
        => work.ThucThiAsync(async () => TongHop(await reports.LaySuKienAsync(tuUtc, denUtc, ct)), ct);

    internal static TongHopBanTraCaDto TongHop(IReadOnlyList<SuKienBanTra> rows)
    {
        const string tm = PhuongThucThanhToan.TienMat;
        const string ck = PhuongThucThanhToan.ChuyenKhoan;
        decimal Tong(string loai, string? pt = null) => rows.Where(s => s.Loai == loai && s.PhuongThuc == pt).Sum(s => s.SoTien);
        int Dem(string loai) => rows.Where(s => s.Loai == loai).Select(s => s.MaChungTu).Distinct().Count();

        return new(Dem("Ban"), Tong("Ban"), Tong("ThuBan", tm), Tong("ThuBan", ck),
            Tong("HoanTra", tm), Tong("HoanTra", ck), Dem("DaoBan"), Tong("DaoThanhToan", tm), Tong("DaoThanhToan", ck));
    }
}
