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
    PaymentProcessorContext payments, ICounterRequestRepository requests, ITaiKhoanRepository taiKhoanRepo)
{
    public async Task<HoaDon> LuuNhapAsync(string maTaiKhoan, string maCa, string? maHoaDon,
        IReadOnlyList<DongBanNhapDto> dongHang, IReadOnlyList<ThanhToanNhapDto> thanhToan,
        DateTime lucUtc, string maYeuCau, CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.BAN_LAP_HD, ct);
        var noiDung = new
        {
            maCa, maHoaDon, dongHang,
            ThanhToan = thanhToan.Select(t => new
            {
                t.PhuongThuc, SoTien = CounterRequest.ChuanHoaTien(t.SoTien), TienKhachDua = CounterRequest.ChuanHoaTien(t.TienKhachDua)
            })
        };
        return await work.ThucThiAsync(() => requests.GhiMotLanAsync("LuuNhapBan", maTaiKhoan, maYeuCau,
            noiDung, GhiAsync, HoaDonDaGhi.Chup, h => h.KhoiPhuc(), ct), ct);

        async Task<HoaDon> GhiAsync()
        {
            CounterTransaction.KiemTraChuCa(await caRepo.KhoaCaMoAsync(maCa, ct), maTaiKhoan);
            var draft = maHoaDon is null ? null : await KhoaHoaDon(maHoaDon, ct);
            if (draft is not null)
            {
                KiemTraHoaDonTrongCa(draft, maCa, maTaiKhoan);
                BatBuocNhap(draft);
            }

            var thanhToanCu = draft?.ThanhToanBans
                .Select(t => new ThanhToanNhapDto(t.PhuongThuc, t.SoTienThanhToan, t.TienKhachDua)) ?? [];
            if (!SapXepThanhToan(thanhToanCu).SequenceEqual(SapXepThanhToan(thanhToan)))
            {
                await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.BAN_THANH_TOAN, ct);
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
        }
    }

    public async Task<HoaDon> XacNhanBanAsync(string maHoaDon, string maCa, string maTaiKhoan,
        DateTime lucUtc, string phienBanNhap, string maYeuCau, CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.BAN_XAC_NHAN, ct);
        return await work.ThucThiAsync(() => requests.GhiMotLanAsync("XacNhanBan", maTaiKhoan, maYeuCau,
            new { maHoaDon, maCa, phienBanNhap }, GhiAsync, HoaDonDaGhi.Chup, h => h.KhoiPhuc(), ct), ct);

        async Task<HoaDon> GhiAsync()
        {
            CounterTransaction.KiemTraChuCa(await caRepo.KhoaCaMoAsync(maCa, ct), maTaiKhoan);
            var hoaDon = await KhoaHoaDon(maHoaDon, ct);
            KiemTraHoaDonTrongCa(hoaDon, maCa, maTaiKhoan);
            BatBuocNhap(hoaDon);
            PhienBanNhap.KiemTra(PhienBanNhap.CuaHoaDon(hoaDon), phienBanNhap);
            foreach (var khoan in hoaDon.ThanhToanBans)
            {
                payments.KiemTraThanhToan(khoan);
            }
            hoaDon.XacNhan(lucUtc);
            await hoaDonRepo.CapNhatAsync(hoaDon, ct);
            await inventory.GhiNhanXuatBanAsync(new(hoaDon.MaHoaDon, lucUtc, DongXuat(hoaDon)), ct);
            return hoaDon;
        }
    }

    public Task<HoaDon> HuyHoaDonAsync(string maHoaDon, string maTaiKhoan, string lyDo, string? maCaDao,
        DateTime lucUtc, CancellationToken ct = default)
        => work.ThucThiAsync(async () =>
        {
            HoaDon hoaDon;
            if (maCaDao is not null)
            {
                var caDao = await caRepo.KhoaCaMoAsync(maCaDao, ct);
                if (lucUtc < caDao.LucMo)
                {
                    throw new BusinessRuleViolationException("Thời điểm đảo tiền mặt phải từ lúc mở ca đảo.");
                }
                hoaDon = await KhoaHoaDon(maHoaDon, ct);
            }
            else
            {
                var banDau = await hoaDonRepo.LayTheoMaAsync(maHoaDon, ct)
                    ?? throw new EntityNotFoundException("Không tìm thấy hóa đơn.");
                if (banDau.TrangThai == TrangThaiChungTu.Nhap)
                {
                    await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.BAN_HUY_HD_NHAP, ct);
                    var actor = await taiKhoanRepo.LayTheoMaAsync(maTaiKhoan, ct);
                    if (actor?.MaVaiTro != "QuanLy")
                    {
                        KiemTraHoaDonTrongCa(banDau, banDau.MaCa, maTaiKhoan);
                        CounterTransaction.KiemTraChuCa(await caRepo.KhoaCaMoAsync(banDau.MaCa, ct), maTaiKhoan);
                    }
                }
                hoaDon = await KhoaHoaDon(maHoaDon, ct);
                if (banDau.TrangThai != hoaDon.TrangThai)
                {
                    throw new ConcurrencyConflictException("Hóa đơn đã đổi trạng thái. Hãy tải lại trước khi hủy.");
                }
            }
            var daXacNhan = hoaDon.TrangThai == TrangThaiChungTu.DaXacNhan;
            if (daXacNhan)
            {
                await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.BAN_HUY_HD_XAC_NHAN, ct);
            }
            else if (maCaDao is not null)
            {
                throw new BusinessRuleViolationException("Chỉ đảo tiền mặt khi hủy hóa đơn đã xác nhận.");
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

    private static IOrderedEnumerable<ThanhToanNhapDto> SapXepThanhToan(IEnumerable<ThanhToanNhapDto> cacKhoan)
        => cacKhoan.OrderBy(t => t.PhuongThuc, StringComparer.Ordinal).ThenBy(t => t.SoTien).ThenBy(t => t.TienKhachDua);

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
