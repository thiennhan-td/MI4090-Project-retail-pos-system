using Pos.Contracts;
using Pos.Contracts.Reports;
using Pos.Domain.Constants;
using Pos.Domain.Exceptions;
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

    /// <summary>
    /// Truy vết một nhà cung cấp từ đầu lịch sử đến trước mốc kết thúc UTC của báo cáo.
    /// Các dòng có TruocKy đóng góp vào dư đầu. Các dòng còn lại thuộc kỳ báo cáo.
    /// </summary>
    public async Task<IReadOnlyList<DongChiTietNhapCongNoDto>> LayChiTietAsync(
        BoLocNhapCongNoDto boLoc, string maTaiKhoan, CancellationToken ct = default)
    {
        var ky = await KiemTraVaChuanHoaKhoangNgayAsync(boLoc, maTaiKhoan, ct);
        var (kyTruyVan, maNhaCungCap) = TaoBoLocTruyVan(boLoc, ky);
        if (maNhaCungCap is null)
        {
            throw new BusinessRuleViolationException("Cần chọn nhà cung cấp để xem chi tiết công nợ.");
        }

        return await repository.LayChiTietNhapCongNoAsync(kyTruyVan.TuUtc, kyTruyVan.DenUtc, maNhaCungCap, ct);
    }

    protected override (KhoangBaoCaoUtcDto Ky, string? MaNhaCungCap) TaoBoLocTruyVan(
        BoLocBaoCaoDto boLoc, KhoangBaoCaoUtcDto ky)
    {
        var maNhaCungCap = (boLoc as BoLocNhapCongNoDto)?.MaNhaCungCap;
        if (maNhaCungCap is not null
            && (maNhaCungCap.Length is < 1 or > 32 || maNhaCungCap.Any(kyTu => kyTu is <= ' ' or > '~')))
        {
            throw new BusinessRuleViolationException("Mã nhà cung cấp không hợp lệ.");
        }

        return (ky, maNhaCungCap);
    }

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
