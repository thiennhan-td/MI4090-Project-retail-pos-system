using Pos.Contracts;
using Pos.Contracts.Reports;
using Pos.Domain.Constants;
using Pos.Domain.Interfaces;
using Pos.Domain.Reports;

namespace Pos.Inventory.Reports;

public sealed record BoLocXuatNhapTonDto(
    DateOnly TuNgay, DateOnly DenNgay, string? MaNhom = null, string? MaHang = null)
    : BoLocBaoCaoDto(TuNgay, DenNgay);

public sealed class BaoCaoXuatNhapTon(
    IInventoryReportRepository repository, IAuthorizationService authorizationService, TimeZoneInfo muiGioCuaHang)
    : BaoCaoTemplateBase<(KhoangBaoCaoUtcDto Ky, string? MaNhom, string? MaHang), IReadOnlyList<DongXuatNhapTonDto>>(
        authorizationService, muiGioCuaHang)
{
    public override string MaQuyenYeuCau => MaQuyenPos.BC_XUAT_NHAP_TON;
    public override string TenBaoCao => "Thống kê xuất, nhập, tồn";

    protected override (KhoangBaoCaoUtcDto Ky, string? MaNhom, string? MaHang) TaoBoLocTruyVan(
        BoLocBaoCaoDto boLoc, KhoangBaoCaoUtcDto ky)
    {
        var boLocHang = boLoc as BoLocXuatNhapTonDto;
        return (ky, boLocHang?.MaNhom, boLocHang?.MaHang);
    }

    protected override Task<IReadOnlyList<DongXuatNhapTonDto>> TruyVanDuLieuNguonAsync(
        (KhoangBaoCaoUtcDto Ky, string? MaNhom, string? MaHang) boLoc, CancellationToken ct)
        => repository.LayXuatNhapTonAsync(boLoc.Ky.TuUtc, boLoc.Ky.DenUtc, boLoc.MaNhom, boLoc.MaHang, ct);

    protected override IReadOnlyList<DongXuatNhapTonDto> TongHopSoLieu(IReadOnlyList<DongXuatNhapTonDto> duLieu)
        => duLieu;

    protected override KetQuaBaoCaoDto ChuyenThanhBangXuat(IReadOnlyList<DongXuatNhapTonDto> ketQua)
        => new(TenBaoCao,
            ["Mã hàng", "Mặt hàng", "Nhóm hàng", "Đơn vị", "Tồn đầu", "Nhập mua ròng", "Xuất bán ròng",
                "Trả nhập lại", "Điều chỉnh tồn", "Tổng nhập", "Tổng xuất", "Tồn cuối"],
            ketQua.Select<DongXuatNhapTonDto, IReadOnlyList<object?>>(dong =>
                [dong.MaHang, dong.TenHang, dong.TenNhom, dong.DonViTinh, dong.TonDau, dong.NhapMuaRong,
                    dong.XuatBanRong, dong.TraNhapLai, dong.DieuChinhTon, dong.TongNhap, dong.TongXuat,
                    dong.TonCuoi]).ToArray());
}
