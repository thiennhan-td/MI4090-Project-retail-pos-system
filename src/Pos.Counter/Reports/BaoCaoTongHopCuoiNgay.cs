using Pos.Contracts;
using Pos.Contracts.Reports;
using Pos.Counter.Services;
using Pos.Domain.Interfaces;
using Pos.Domain.Constants;

namespace Pos.Counter.Reports;

public sealed record DuLieuCuoiNgay(IReadOnlyList<SuKienBanTra> SuKien, TongHopChiNccTrongKyDto ChiNcc,
    IReadOnlyList<IReadOnlyList<object?>>? Bang = null);

public sealed class BaoCaoTongHopCuoiNgay(IAuthorizationService auth, TimeZoneInfo muiGio,
    ICounterReportRepository reports, IInventoryReportQueryService kho, IUnitOfWork work)
    : BaoCaoTemplateBase<KhoangBaoCaoUtcDto, DuLieuCuoiNgay>(auth, muiGio)
{
    public override string MaQuyenYeuCau => MaQuyenPos.BC_CUOI_NGAY;
    public override string TenBaoCao => "Báo cáo cuối ngày";

    protected override KhoangBaoCaoUtcDto TaoBoLocTruyVan(BoLocBaoCaoDto boLoc, KhoangBaoCaoUtcDto ky)
        => boLoc.TuNgay == boLoc.DenNgay ? ky
            : throw new Pos.Domain.Exceptions.BusinessRuleViolationException("Chọn một ngày cho báo cáo cuối ngày.");

    protected override Task<DuLieuCuoiNgay> TruyVanDuLieuNguonAsync(KhoangBaoCaoUtcDto ky, CancellationToken ct)
        => work.ThucThiAsync(async () => new DuLieuCuoiNgay(
            await reports.LaySuKienAsync(ky.TuUtc, ky.DenUtc, ct),
            await kho.LayTongHopChiNccTheoKyAsync(ky.TuUtc, ky.DenUtc, ct)), ct);

    protected override DuLieuCuoiNgay TongHopSoLieu(DuLieuCuoiNgay duLieu)
    {
        var tong = CounterReportQueryService.TongHop(duLieu.SuKien);
        List<IReadOnlyList<object?>> bang =
        [
            new object?[] { "Số hóa đơn xác nhận", tong.SoHoaDon, null },
            new object?[] { "Giá trị bán", null, tong.TongGiaTriBan },
            new object?[] { "Thu bán tiền mặt", null, tong.ThuBanTienMat },
            new object?[] { "Thu bán chuyển khoản", null, tong.ThuBanChuyenKhoan },
            new object?[] { "Hoàn tiền mặt", null, tong.HoanTienMat },
            new object?[] { "Hoàn chuyển khoản", null, tong.HoanChuyenKhoan },
            new object?[] { "Hóa đơn đảo", tong.SoHoaDonHuy, null },
            new object?[] { "Đảo tiền mặt", null, tong.DaoHuyTienMat },
            new object?[] { "Đảo chuyển khoản", null, tong.DaoHuyChuyenKhoan },
            new object?[] { "Chi NCC từ két", null, duLieu.ChiNcc.ChiTuKetCa },
            new object?[] { "Chi NCC từ quỹ bằng tiền mặt", null, duLieu.ChiNcc.ChiQuyCuaHangTienMat },
            new object?[] { "Chi NCC từ quỹ bằng chuyển khoản", null, duLieu.ChiNcc.ChiQuyCuaHangChuyenKhoan }
        ];
        foreach (var nhom in duLieu.SuKien.Where(s => s.Loai is "ThuKet" or "ChiKet" or "ThuQuy" or "ChiQuy")
            .GroupBy(s => (s.Loai, s.PhuongThuc)))
        {
            var loai = nhom.Key.Loai switch
            {
                "ThuKet" => "Thu riêng từ két",
                "ChiKet" => "Chi riêng từ két",
                "ThuQuy" => "Thu riêng quỹ cửa hàng",
                _ => "Chi riêng quỹ cửa hàng"
            };
            var phuongThuc = nhom.Key.PhuongThuc == PhuongThucThanhToan.TienMat ? "tiền mặt" : "chuyển khoản";
            bang.Add([$"{loai} ({phuongThuc})", null, nhom.Sum(s => s.SoTien)]);
        }
        return duLieu with { Bang = bang };
    }

    protected override KetQuaBaoCaoDto ChuyenThanhBangXuat(DuLieuCuoiNgay ketQua)
        => new(TenBaoCao, ["Chỉ tiêu", "Số chứng từ", "Số tiền"], ketQua.Bang!);
}
