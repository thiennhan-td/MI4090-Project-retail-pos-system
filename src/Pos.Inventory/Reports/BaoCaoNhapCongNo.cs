using Pos.Contracts;
using Pos.Contracts.Reports;
using Pos.Domain.Constants;
using Pos.Domain.Interfaces;
using Pos.Domain.Reports;

namespace Pos.Inventory.Reports;

public sealed record BoLocNhapCongNoDto(DateOnly TuNgay, DateOnly DenNgay, string? MaNhaCungCap = null)
    : BoLocBaoCaoDto(TuNgay, DenNgay);

public sealed class BaoCaoNhapCongNo(
    IInventoryReportRepository repository, IAuthorizationService authorizationService, TimeZoneInfo muiGioCuaHang)
    : BaoCaoTemplateBase<(KhoangBaoCaoUtcDto Ky, string? MaNhaCungCap), IReadOnlyList<DongNhapCongNoDto>>(
        authorizationService, muiGioCuaHang)
{
    public override string MaQuyenYeuCau => MaQuyenPos.BC_NHAP_CONG_NO;
    public override string TenBaoCao => "Nhập hàng và công nợ NCC";

    protected override (KhoangBaoCaoUtcDto Ky, string? MaNhaCungCap) TaoBoLocTruyVan(
        BoLocBaoCaoDto boLoc, KhoangBaoCaoUtcDto ky)
        => (ky, (boLoc as BoLocNhapCongNoDto)?.MaNhaCungCap);

    protected override Task<IReadOnlyList<DongNhapCongNoDto>> TruyVanDuLieuNguonAsync(
        (KhoangBaoCaoUtcDto Ky, string? MaNhaCungCap) boLoc, CancellationToken ct)
        => repository.LayNhapCongNoAsync(boLoc.Ky.TuUtc, boLoc.Ky.DenUtc, boLoc.MaNhaCungCap, ct);

    protected override IReadOnlyList<DongNhapCongNoDto> TongHopSoLieu(IReadOnlyList<DongNhapCongNoDto> duLieu)
        => duLieu;

    protected override KetQuaBaoCaoDto ChuyenThanhBangXuat(IReadOnlyList<DongNhapCongNoDto> ketQua)
        => new(TenBaoCao,
            ["Mã NCC", "Nhà cung cấp", "Dư đầu", "Nhập", "Đảo nhập", "Đã trả", "Dư cuối"],
            ketQua.Select<DongNhapCongNoDto, IReadOnlyList<object?>>(dong =>
                [dong.MaNhaCungCap, dong.TenNhaCungCap, dong.DuDau, dong.GiaTriNhap, dong.GiaTriDaoNhap,
                    dong.DaTra, dong.DuCuoi]).ToArray());
}
