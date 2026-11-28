using Pos.Contracts;
using Pos.Counter.Strategies;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;

namespace Pos.Counter.Services;

public sealed record DongBanNhapDto(string MaHang, int SoLuong);
public sealed record ThanhToanNhapDto(string PhuongThuc, decimal SoTien, decimal? TienKhachDua);

public sealed class SaleService(
    IHoaDonRepository hoaDonRepo,
    ICaBanRepository caRepo,
    IPhieuTraRepository traRepo,
    IInventoryService inventory,
    ICatalogQueryService catalog,
    IAuthorizationService auth,
    IUnitOfWork work,
    PaymentProcessorContext payments)
{
    public async Task<HoaDon> LuuNhapAsync(string maTaiKhoan, string maCa, string? maHoaDon,
        IReadOnlyList<DongBanNhapDto> dongHang, IReadOnlyList<ThanhToanNhapDto> thanhToan,
        DateTime lucUtc, CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.BAN_LAP_HD, ct);
        if (thanhToan.Count > 0)
        {
            await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.BAN_THANH_TOAN, ct);
        }
        return await work.ThucThiAsync(async () =>
        {
            CounterTransaction.KiemTraChuCa(await caRepo.KhoaCaMoAsync(maCa, ct), maTaiKhoan);
            var draft = maHoaDon is null ? null : await KhoaHoaDon(maHoaDon, ct);
            if (draft is not null)
            {
                KiemTraHoaDonTrongCa(draft, maCa, maTaiKhoan);
                BatBuocNhap(draft);
            }

            var hoaDon = new HoaDon(draft?.MaHoaDon ?? await hoaDonRepo.SinhMaAsync(ct), maCa,
                maTaiKhoan, draft?.LucLap ?? lucUtc);
            foreach (var dong in dongHang)
            {
                var matHang = await catalog.LayMatHangDangHoatDongAsync(dong.MaHang, ct)
                    ?? throw new EntityNotFoundException($"Mặt hàng {dong.MaHang} không hoạt động.");
                hoaDon.ThemDongBan(new(await hoaDonRepo.SinhMaDongAsync(ct), hoaDon.MaHoaDon, dong.MaHang,
                    dong.SoLuong, matHang.GiaBanHienHanh));
            }
            foreach (var khoan in thanhToan)
            {
                hoaDon.ThemThanhToanBan(payments.TaoThanhToan(await hoaDonRepo.SinhMaThanhToanAsync(ct),
                    hoaDon.MaHoaDon, khoan.PhuongThuc, khoan.SoTien, khoan.TienKhachDua));
            }

            if (draft is null)
            {
                await hoaDonRepo.ThemAsync(hoaDon, ct);
            }
            else
            {
                await hoaDonRepo.CapNhatAsync(hoaDon, ct);
            }
            return hoaDon;
        }, ct);
    }

    public async Task<HoaDon> XacNhanBanAsync(string maHoaDon, string maCa, string maTaiKhoan,
        DateTime lucUtc, CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.BAN_XAC_NHAN, ct);
        return await work.ThucThiAsync(async () =>
        {
            CounterTransaction.KiemTraChuCa(await caRepo.KhoaCaMoAsync(maCa, ct), maTaiKhoan);
            var hoaDon = await KhoaHoaDon(maHoaDon, ct);
            KiemTraHoaDonTrongCa(hoaDon, maCa, maTaiKhoan);
            BatBuocNhap(hoaDon);
            foreach (var khoan in hoaDon.ThanhToanBans)
            {
                payments.KiemTraThanhToan(khoan);
            }
            hoaDon.XacNhan(lucUtc);
            await hoaDonRepo.CapNhatAsync(hoaDon, ct);
            await inventory.GhiNhanXuatBanAsync(new(hoaDon.MaHoaDon, lucUtc, DongXuat(hoaDon)), ct);
            return hoaDon;
        }, ct);
    }

    public Task<HoaDon> HuyHoaDonAsync(string maHoaDon, string maTaiKhoan, string lyDo, string? maCaDao,
        DateTime lucUtc, CancellationToken ct = default)
        => work.ThucThiAsync(async () =>
        {
            if (maCaDao is not null)
            {
                await caRepo.KhoaCaMoAsync(maCaDao, ct);
            }
            var hoaDon = await KhoaHoaDon(maHoaDon, ct);
            var daXacNhan = hoaDon.TrangThai == TrangThaiChungTu.DaXacNhan;
            await auth.KiemTraQuyenAsync(maTaiKhoan,
                daXacNhan ? MaQuyenPos.BAN_HUY_HD_XAC_NHAN : MaQuyenPos.BAN_HUY_HD_NHAP, ct);
            if (!daXacNhan && hoaDon.TrangThai == TrangThaiChungTu.Nhap && hoaDon.MaNguoiLap != maTaiKhoan)
            {
                // Quyền hủy nháp và quyền xem toàn cửa hàng độc lập.
                await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.BAN_HUY_HD_XAC_NHAN, ct);
            }
            hoaDon.Huy(maTaiKhoan, lucUtc, lyDo, maCaDao,
                daXacNhan && await traRepo.CoPhieuTraHieuLucAsync(maHoaDon, ct));
            await hoaDonRepo.CapNhatAsync(hoaDon, ct);
            if (daXacNhan)
            {
                await inventory.GhiNhanDaoHuyBanAsync(new(maHoaDon, lucUtc, DongXuat(hoaDon)), ct);
            }
            return hoaDon;
        }, ct);

    private async Task<HoaDon> KhoaHoaDon(string ma, CancellationToken ct)
        => await hoaDonRepo.KhoaTheoMaAsync(ma, ct) ?? throw new EntityNotFoundException("Không tìm thấy hóa đơn.");

    private static void KiemTraHoaDonTrongCa(HoaDon hoaDon, string maCa, string maTaiKhoan)
    {
        if (hoaDon.MaCa != maCa || hoaDon.MaNguoiLap != maTaiKhoan)
        {
            throw new BusinessRuleViolationException("Hóa đơn không thuộc ca và tài khoản thực hiện.");
        }
    }

    private static void BatBuocNhap(HoaDon hoaDon)
    {
        if (hoaDon.TrangThai != TrangThaiChungTu.Nhap)
        {
            throw new InvalidStateTransitionException("Hóa đơn không còn ở trạng thái nháp.");
        }
    }

    private static DongXuatBanDto[] DongXuat(HoaDon hoaDon) => hoaDon.DongBans
        .OrderBy(dong => dong.MaHang, StringComparer.Ordinal)
        .ThenBy(dong => dong.MaDongBan, StringComparer.Ordinal)
        .Select(dong => new DongXuatBanDto(dong.MaDongBan, dong.MaHang, dong.SoLuongBan)).ToArray();
}
