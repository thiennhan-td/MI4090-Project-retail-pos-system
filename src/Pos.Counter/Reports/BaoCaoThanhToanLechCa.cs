using Pos.Contracts;
using Pos.Contracts.Reports;
using Pos.Counter.Services;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Interfaces;

namespace Pos.Counter.Reports;

public sealed record DuLieuThanhToanLechCa(IReadOnlyList<SuKienBanTra> SuKien, IReadOnlyList<CaBan> CaDaChot,
    TongHopChiNccTrongKyDto ChiNcc, IReadOnlyList<IReadOnlyList<object?>>? Bang = null);

public sealed class BaoCaoThanhToanLechCa(IAuthorizationService auth, TimeZoneInfo muiGio,
    ICounterReportRepository reports, ICaBanRepository caRepo, IInventoryReportQueryService kho, IUnitOfWork work)
    : BaoCaoTemplateBase<KhoangBaoCaoUtcDto, DuLieuThanhToanLechCa>(auth, muiGio)
{
    public override string MaQuyenYeuCau => MaQuyenPos.BC_THANH_TOAN_CA;
    public override string TenBaoCao => "Thống kê thanh toán và chênh lệch ca";

    protected override KhoangBaoCaoUtcDto TaoBoLocTruyVan(BoLocBaoCaoDto boLoc, KhoangBaoCaoUtcDto ky) => ky;

    protected override Task<DuLieuThanhToanLechCa> TruyVanDuLieuNguonAsync(KhoangBaoCaoUtcDto ky, CancellationToken ct)
        => work.ThucThiAsync(async () =>
        {
            var suKien = await reports.LaySuKienAsync(ky.TuUtc, ky.DenUtc, ct);
            var ca = await caRepo.LayTrongKyAsync(ky.TuUtc, ky.DenUtc, ct);
            var chiNcc = await kho.LayTongHopChiNccTheoKyAsync(ky.TuUtc, ky.DenUtc, ct);
            return new DuLieuThanhToanLechCa(suKien,
                ca.Where(c => c.LucDong >= ky.TuUtc && c.LucDong < ky.DenUtc).ToArray(), chiNcc);
        }, ct);

    protected override DuLieuThanhToanLechCa TongHopSoLieu(DuLieuThanhToanLechCa duLieu)
    {
        List<IReadOnlyList<object?>> bang = [];
        foreach (var pt in new[] { PhuongThucThanhToan.TienMat, PhuongThucThanhToan.ChuyenKhoan })
        {
            var nhom = duLieu.SuKien.Where(s => s.PhuongThuc == pt).ToLookup(s => s.Loai);
            decimal Tong(string loai) => nhom[loai].Sum(s => s.SoTien);
            var chiNcc = pt == PhuongThucThanhToan.TienMat
                ? duLieu.ChiNcc.ChiTuKetCa + duLieu.ChiNcc.ChiQuyCuaHangTienMat
                : duLieu.ChiNcc.ChiQuyCuaHangChuyenKhoan;
            bang.Add([pt == PhuongThucThanhToan.TienMat ? "Tiền mặt" : "Chuyển khoản",
                Tong("ThuBan"), Tong("HoanTra"), Tong("DaoThanhToan"), Tong("ThuKet") + Tong("ThuQuy"),
                Tong("ChiKet") + Tong("ChiQuy"), chiNcc, null, null, null, null]);
        }
        foreach (var ca in duLieu.CaDaChot)
        {
            bang.Add([ca.MaCa, null, null, null, null, null, null,
                ca.TienMatDauCa, ca.TienDuKienKhiChot, ca.TienThucDem, ca.ChenhLech]);
        }
        bang.Add(["Tổng chênh lệch", null, null, null, null, null, null, null, null, null,
            duLieu.CaDaChot.Sum(ca => ca.ChenhLech ?? 0m)]);
        return duLieu with { Bang = bang };
    }

    protected override KetQuaBaoCaoDto ChuyenThanhBangXuat(DuLieuThanhToanLechCa ketQua) => new(TenBaoCao,
        ["Phương thức / ca", "Thu bán", "Hoàn trả", "Đảo thanh toán", "Thu riêng", "Chi riêng", "Chi NCC",
            "Tiền đầu ca", "Dự kiến khi chốt", "Thực đếm", "Chênh lệch"], ketQua.Bang!);
}
