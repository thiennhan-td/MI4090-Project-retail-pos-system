using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Interfaces;

namespace Pos.Inventory.Services;

public sealed class CatalogQueryService(IDanhMucRepository repository) : ICatalogQueryService
{
    public async Task<MatHangBanDto?> LayMatHangDangHoatDongAsync(string maHang, CancellationToken ct)
    {
        var hang = await repository.LayMatHangAsync(maHang, ct);
        if (hang is null || hang.TrangThai != TrangThaiChungTu.HoatDong)
        {
            return null;
        }

        var nhom = await repository.LayNhomHangAsync(ct);
        return nhom.Any(x => x.MaNhom == hang.MaNhom && DangHoatDong(x)) ? Dto(hang) : null;
    }

    public async Task<IReadOnlyList<MatHangBanDto>> TimKiemMatHangBanAsync(string? tuKhoa, string? maNhom,
        CancellationToken ct)
    {
        var nhom = (await repository.LayNhomHangAsync(ct)).Where(DangHoatDong)
            .Select(x => x.MaNhom).ToHashSet(StringComparer.Ordinal);
        var hang = await repository.TimMatHangAsync(tuKhoa?.Trim(), maNhom, ct);
        return hang.Where(x => x.TrangThai == TrangThaiChungTu.HoatDong && nhom.Contains(x.MaNhom))
            .Select(Dto).ToArray();
    }

    public async Task<IReadOnlyList<NhomHangDto>> LayNhomHangHoatDongAsync(CancellationToken ct)
        => (await repository.LayNhomHangAsync(ct)).Where(DangHoatDong)
            .Select(x => new NhomHangDto(x.MaNhom, x.TenNhom)).ToArray();

    private static bool DangHoatDong(NhomHang nhom) => nhom.TrangThai == TrangThaiChungTu.HoatDong;

    private static MatHangBanDto Dto(MatHang hang) => new(hang.MaHang, hang.MaNhom, hang.TenHang,
        hang.DonViTinh, hang.GiaBanHienHanh);
}
