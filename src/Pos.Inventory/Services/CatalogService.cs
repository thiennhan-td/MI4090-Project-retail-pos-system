using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;

namespace Pos.Inventory.Services;

public sealed record LuuMatHangDto(string? MaHang, string MaNhom, string TenHang, string DonViTinh,
    decimal GiaBanHienHanh);

public sealed class CatalogService(IQuanLyDanhMucRepository repository, IUnitOfWork work,
    IAuthorizationService authorization)
{
    public async Task<IReadOnlyList<NhomHang>> LayNhomHangAsync(string maNguoiThaoTac, CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.DM_NHOM_XEM, ct);
        return await repository.LayNhomHangAsync(ct);
    }

    public async Task<IReadOnlyList<MatHang>> TimMatHangAsync(string maNguoiThaoTac, string? tuKhoa,
        string? maNhom, CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.DM_HANG_XEM, ct);
        return await repository.TimMatHangAsync(tuKhoa?.Trim(), maNhom, ct);
    }

    public async Task<IReadOnlyList<NhaCungCap>> LayNhaCungCapAsync(string maNguoiThaoTac, CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.DM_NCC_XEM, ct);
        return await repository.LayNhaCungCapAsync(ct);
    }

    public async Task<NhomHang> LuuNhomAsync(string maNguoiThaoTac, string? maNhom, string tenNhom,
        CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.DM_NHOM_QL, ct);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            if (maNhom is null)
            {
                var moi = new NhomHang(await repository.SinhMaNhomAsync(ct), tenNhom.Trim());
                await repository.ThemNhomAsync(moi, ct);
                return moi;
            }

            var nhom = await KhoaNhomAsync(maNhom, ct);
            nhom.DoiTen(tenNhom.Trim());
            await repository.CapNhatNhomAsync(nhom, ct);
            return nhom;
        }, ct);
    }

    public async Task NgungNhomAsync(string maNguoiThaoTac, string maNhom, CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.DM_NHOM_QL, ct);
        await InventoryTransaction.ChayAsync(work, async () =>
        {
            var nhom = await KhoaNhomAsync(maNhom, ct);
            if (await repository.CoMatHangDangHoatDongAsync(maNhom, ct))
            {
                throw new BusinessRuleViolationException("Chuyển nhóm hoặc ngừng dùng các mặt hàng đang hoạt động trước.");
            }

            nhom.NgungSuDung();
            await repository.CapNhatNhomAsync(nhom, ct);
        }, ct);
    }

    public async Task<MatHang> LuuHangAsync(string maNguoiThaoTac, LuuMatHangDto yeuCau, CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.DM_HANG_QL, ct);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var nhom = await KhoaNhomAsync(yeuCau.MaNhom, ct);
            if (nhom.TrangThai != TrangThaiChungTu.HoatDong)
            {
                throw new BusinessRuleViolationException("Chọn một nhóm hàng đang hoạt động.");
            }

            if (yeuCau.MaHang is null)
            {
                var moi = new MatHang(await repository.SinhMaHangAsync(ct), nhom.MaNhom,
                    yeuCau.TenHang.Trim(), yeuCau.DonViTinh.Trim(), yeuCau.GiaBanHienHanh);
                await repository.ThemHangAsync(moi, ct);
                return moi;
            }

            var hang = await KhoaHangAsync(yeuCau.MaHang, ct);
            hang.CapNhatDanhMuc(nhom.MaNhom, yeuCau.TenHang.Trim(), yeuCau.DonViTinh.Trim(), yeuCau.GiaBanHienHanh);
            await repository.CapNhatHangAsync(hang, ct);
            return hang;
        }, ct);
    }

    public async Task NgungHangAsync(string maNguoiThaoTac, string maHang, CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.DM_HANG_QL, ct);
        await InventoryTransaction.ChayAsync(work, async () =>
        {
            var hang = await KhoaHangAsync(maHang, ct);
            hang.NgungSuDung();
            await repository.CapNhatHangAsync(hang, ct);
        }, ct);
    }

    public async Task<NhaCungCap> LuuNhaCungCapAsync(string maNguoiThaoTac, string? maNhaCungCap,
        string tenNhaCungCap, string? thongTinLienHe, CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.DM_NCC_QL, ct);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            var ma = maNhaCungCap ?? await repository.SinhMaNhaCungCapAsync(ct);
            var nhaCungCap = new NhaCungCap(ma, tenNhaCungCap.Trim(), thongTinLienHe?.Trim());
            if (maNhaCungCap is null)
            {
                await repository.ThemNhaCungCapAsync(nhaCungCap, ct);
            }
            else
            {
                await repository.CapNhatNhaCungCapAsync(nhaCungCap, ct);
            }

            return nhaCungCap;
        }, ct);
    }

    private async Task<NhomHang> KhoaNhomAsync(string maNhom, CancellationToken ct)
        => await repository.KhoaNhomHangAsync(maNhom, ct) ?? throw new EntityNotFoundException("Không tìm thấy nhóm hàng.");

    private async Task<MatHang> KhoaHangAsync(string maHang, CancellationToken ct)
    {
        await repository.KhoaMatHangAsync(maHang, ct);
        return await repository.LayMatHangAsync(maHang, ct) ?? throw new EntityNotFoundException("Không tìm thấy mặt hàng.");
    }
}
