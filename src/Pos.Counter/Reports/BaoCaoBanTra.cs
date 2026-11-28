using Pos.Contracts;
using Pos.Contracts.Reports;
using Pos.Counter.Services;
using Pos.Domain.Constants;
using Pos.Domain.Interfaces;

namespace Pos.Counter.Reports;

public sealed record BoLocBanTraDto(DateOnly TuNgay, DateOnly DenNgay, string? MaHang)
    : BoLocBaoCaoDto(TuNgay, DenNgay);

public sealed record TruyVanBanTraDto(KhoangBaoCaoUtcDto Ky, string? MaHang);
public sealed record DuLieuBaoCaoBanTra(IReadOnlyList<SuKienBanTra> SuKien,
    IReadOnlyList<IReadOnlyList<object?>>? Bang = null);

public sealed class BaoCaoBanTra(IAuthorizationService auth, TimeZoneInfo muiGio,
    ICounterReportRepository reports, IUnitOfWork work)
    : BaoCaoTemplateBase<TruyVanBanTraDto, DuLieuBaoCaoBanTra>(auth, muiGio)
{
    public override string MaQuyenYeuCau => MaQuyenPos.BC_BAN_TRA;
    public override string TenBaoCao => "Thống kê bán hàng và trả hàng";

    protected override TruyVanBanTraDto TaoBoLocTruyVan(BoLocBaoCaoDto boLoc, KhoangBaoCaoUtcDto ky)
        => new(ky, (boLoc as BoLocBanTraDto)?.MaHang);

    protected override Task<DuLieuBaoCaoBanTra> TruyVanDuLieuNguonAsync(TruyVanBanTraDto loc, CancellationToken ct)
        => work.ThucThiAsync(async () =>
        {
            var rows = await reports.LaySuKienAsync(loc.Ky.TuUtc, loc.Ky.DenUtc, ct);
            return new DuLieuBaoCaoBanTra(rows.Where(row => row.MaHang is not null &&
                (string.IsNullOrWhiteSpace(loc.MaHang) || row.MaHang == loc.MaHang)).ToArray());
        }, ct);

    protected override DuLieuBaoCaoBanTra TongHopSoLieu(DuLieuBaoCaoBanTra duLieu)
    {
        var bang = duLieu.SuKien.GroupBy(row => row.MaHang).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => Tong(g.Key!, g)).ToList();
        bang.Add(Tong("Tổng cộng", duLieu.SuKien));
        return duLieu with { Bang = bang };
    }

    private static IReadOnlyList<object?> Tong(string ma, IEnumerable<SuKienBanTra> rows)
    {
        var byLoai = rows.ToLookup(s => s.Loai);
        var slBan = byLoai["Ban"].Sum(s => (long)s.SoLuong);
        var slDao = byLoai["DaoBan"].Sum(s => (long)s.SoLuong);
        var slTra = byLoai["Tra"].Sum(s => (long)s.SoLuong);
        var ban = byLoai["Ban"].Sum(s => s.SoTien);
        var dao = byLoai["DaoBan"].Sum(s => s.SoTien);
        var tra = byLoai["Tra"].Sum(s => s.SoTien);
        return [ma, slBan, slDao, slTra, slBan - slDao - slTra, ban, dao, ban - dao, tra, ban - dao - tra];
    }

    protected override KetQuaBaoCaoDto ChuyenThanhBangXuat(DuLieuBaoCaoBanTra ketQua) => new(TenBaoCao,
        ["Mã hàng", "SL bán", "SL đảo", "SL trả", "SL bán thuần", "Giá trị bán", "Giá trị đảo",
            "Bán sau đảo", "Giá trị trả", "Bán thuần"], ketQua.Bang!);
}
