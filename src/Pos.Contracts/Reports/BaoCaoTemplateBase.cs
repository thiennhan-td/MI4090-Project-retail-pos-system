using Pos.Domain.Exceptions;

namespace Pos.Contracts.Reports;

public abstract class BaoCaoTemplateBase<TFilter, TResult>(
    IAuthorizationService authorizationService, TimeZoneInfo muiGioCuaHang) : IBaoCaoNghiepVu
{
    public abstract string MaQuyenYeuCau { get; }
    public abstract string TenBaoCao { get; }

    public async Task<KetQuaBaoCaoDto> ThucThiAsync(
        BoLocBaoCaoDto boLoc, string maTaiKhoan, CancellationToken ct)
    {
        await authorizationService.KiemTraQuyenAsync(maTaiKhoan, MaQuyenYeuCau, ct);
        var ky = ChuanHoaKhoangNgay(boLoc);
        var duLieu = await TruyVanDuLieuNguonAsync(TaoBoLocTruyVan(boLoc, ky), ct);
        var tongHop = TongHopSoLieu(duLieu);
        return ChuyenThanhBangXuat(tongHop);
    }

    private KhoangBaoCaoUtcDto ChuanHoaKhoangNgay(BoLocBaoCaoDto boLoc)
    {
        if (boLoc.TuNgay > boLoc.DenNgay || boLoc.DenNgay == DateOnly.MaxValue)
        {
            throw new BusinessRuleViolationException("Khoảng ngày báo cáo không hợp lệ.");
        }

        return new(
            TimeZoneInfo.ConvertTimeToUtc(boLoc.TuNgay.ToDateTime(TimeOnly.MinValue), muiGioCuaHang),
            TimeZoneInfo.ConvertTimeToUtc(boLoc.DenNgay.AddDays(1).ToDateTime(TimeOnly.MinValue), muiGioCuaHang));
    }

    protected abstract TFilter TaoBoLocTruyVan(BoLocBaoCaoDto boLoc, KhoangBaoCaoUtcDto ky);
    protected abstract Task<TResult> TruyVanDuLieuNguonAsync(TFilter boLoc, CancellationToken ct);
    protected abstract TResult TongHopSoLieu(TResult duLieu);
    protected abstract KetQuaBaoCaoDto ChuyenThanhBangXuat(TResult ketQua);
}
