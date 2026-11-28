using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;

namespace Pos.Counter.Services;

public sealed record DongTraNhapDto(string MaDongBan, int SoLuongTra, int SoLuongNhapLai);
public sealed record KhoanHoanNhapDto(string PhuongThuc, decimal SoTien);

public sealed class ReturnService(
    IPhieuTraRepository traRepo, IHoaDonRepository hoaDonRepo, ICaBanRepository caRepo,
    IInventoryService inventory, IAuthorizationService auth, IUnitOfWork work)
{
    public async Task<PhieuTra> LapPhieuTraAsync(string maHoaDon, string maCa, string maTaiKhoan,
        IReadOnlyList<DongTraNhapDto> dongTra, IReadOnlyList<KhoanHoanNhapDto> khoanHoan,
        DateTime lucUtc, CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.TRA_LAP_PHIEU, ct);
        return await work.ThucThiAsync(async () =>
        {
            CounterTransaction.KiemTraChuCa(await caRepo.KhoaCaMoAsync(maCa, ct), maTaiKhoan);
            var hoaDon = await KhoaHoaDonGoc(maHoaDon, ct);
            var phieu = new PhieuTra(await traRepo.SinhMaAsync(ct), maHoaDon, maCa, maTaiKhoan, lucUtc);
            await DienNoiDungAsync(phieu, dongTra, khoanHoan, ct);
            await TinhGiaTriTraAsync(hoaDon, phieu, ct);
            await traRepo.ThemAsync(phieu, ct);
            return phieu;
        }, ct);
    }

    public async Task<PhieuTra> CapNhatNhapAsync(string maPhieuTra, string maCa, string maTaiKhoan,
        string phienBanNhap, IReadOnlyList<DongTraNhapDto> dongTra, IReadOnlyList<KhoanHoanNhapDto> khoanHoan,
        CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.TRA_LAP_PHIEU, ct);
        return await work.ThucThiAsync(async () =>
        {
            CounterTransaction.KiemTraChuCa(await caRepo.KhoaCaMoAsync(maCa, ct), maTaiKhoan);
            var nhap = await LayNhapCuaNguoiNhan(maPhieuTra, maCa, maTaiKhoan, ct);
            PhienBanNhap.KiemTra(PhienBanNhap.CuaPhieuTra(nhap), phienBanNhap);
            var hoaDon = await KhoaHoaDonGoc(nhap.MaHoaDon, ct);
            var phieu = new PhieuTra(nhap.MaPhieuTra, nhap.MaHoaDon, nhap.MaCaNhan, nhap.MaNguoiNhan, nhap.LucLap);
            await DienNoiDungAsync(phieu, dongTra, khoanHoan, ct);
            await TinhGiaTriTraAsync(hoaDon, phieu, ct);
            await traRepo.CapNhatAsync(phieu, ct);
            return phieu;
        }, ct);
    }

    private async Task DienNoiDungAsync(PhieuTra phieu, IReadOnlyList<DongTraNhapDto> dongTra,
        IReadOnlyList<KhoanHoanNhapDto> khoanHoan, CancellationToken ct)
    {
        foreach (var dong in dongTra)
        {
            phieu.ThemDongTra(new(await traRepo.SinhMaDongAsync(ct), phieu.MaPhieuTra, dong.MaDongBan,
                dong.SoLuongTra, dong.SoLuongNhapLai));
        }
        foreach (var khoan in khoanHoan)
        {
            phieu.ThemKhoanHoan(new(await traRepo.SinhMaHoanAsync(ct), phieu.MaPhieuTra,
                khoan.PhuongThuc, khoan.SoTien));
        }
    }

    public async Task<PhieuTra> XacNhanTraAsync(string maPhieuTra, string maCa, string maTaiKhoan,
        DateTime lucUtc, string phienBanNhap, CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.TRA_XAC_NHAN, ct);
        return await work.ThucThiAsync(async () =>
        {
            CounterTransaction.KiemTraChuCa(await caRepo.KhoaCaMoAsync(maCa, ct), maTaiKhoan);
            var phieu = await LayNhapCuaNguoiNhan(maPhieuTra, maCa, maTaiKhoan, ct);
            PhienBanNhap.KiemTra(PhienBanNhap.CuaPhieuTra(phieu), phienBanNhap);
            // Mọi lần xác nhận trả và hủy bán đều khóa cùng một hóa đơn gốc.
            var hoaDon = await KhoaHoaDonGoc(phieu.MaHoaDon, ct);
            if (lucUtc < hoaDon.LucXacNhan)
            {
                throw new BusinessRuleViolationException("Không thể nhận trả trước lúc xác nhận hóa đơn gốc.");
            }
            phieu.XacNhan(lucUtc, await TinhGiaTriTraAsync(hoaDon, phieu, ct));
            await traRepo.CapNhatAsync(phieu, ct);

            var maHangTheoDong = hoaDon.DongBans.ToDictionary(dong => dong.MaDongBan, dong => dong.MaHang);
            var nhapLai = phieu.DongTras.Where(dong => dong.SoLuongNhapLai > 0)
                .Select(dong => new DongNhapTraDto(dong.MaDongTra, maHangTheoDong[dong.MaDongBan], dong.SoLuongNhapLai))
                .OrderBy(dong => dong.MaHang, StringComparer.Ordinal)
                .ThenBy(dong => dong.MaDongTra, StringComparer.Ordinal).ToArray();
            if (nhapLai.Length > 0)
            {
                await inventory.GhiNhanNhapTraAsync(new(maPhieuTra, lucUtc, nhapLai), ct);
            }
            return phieu;
        }, ct);
    }

    public async Task XoaNhapAsync(string maPhieuTra, string maCa, string maTaiKhoan, string phienBanNhap,
        CancellationToken ct = default)
    {
        await auth.KiemTraQuyenAsync(maTaiKhoan, MaQuyenPos.TRA_LAP_PHIEU, ct);
        await work.ThucThiAsync(async () =>
        {
            CounterTransaction.KiemTraChuCa(await caRepo.KhoaCaMoAsync(maCa, ct), maTaiKhoan);
            var nhap = await LayNhapCuaNguoiNhan(maPhieuTra, maCa, maTaiKhoan, ct);
            PhienBanNhap.KiemTra(PhienBanNhap.CuaPhieuTra(nhap), phienBanNhap);
            await traRepo.XoaNhapAsync(maPhieuTra, ct);
            return true;
        }, ct);
    }

    private async Task<PhieuTra> LayNhapCuaNguoiNhan(string ma, string maCa, string maTaiKhoan, CancellationToken ct)
    {
        var phieu = await traRepo.KhoaTheoMaAsync(ma, ct) ?? throw new EntityNotFoundException("Không tìm thấy phiếu trả.");
        if (phieu.MaCaNhan != maCa || phieu.MaNguoiNhan != maTaiKhoan || !phieu.CoTheHuy())
        {
            throw new InvalidStateTransitionException("Chỉ được thao tác trên phiếu trả nháp của mình trong ca hiện tại.");
        }
        return phieu;
    }

    private async Task<HoaDon> KhoaHoaDonGoc(string ma, CancellationToken ct)
    {
        var hoaDon = await hoaDonRepo.KhoaTheoMaAsync(ma, ct) ?? throw new EntityNotFoundException("Không tìm thấy hóa đơn gốc.");
        if (hoaDon.TrangThai != TrangThaiChungTu.DaXacNhan)
        {
            throw new InvalidStateTransitionException("Hóa đơn gốc phải đang có hiệu lực.");
        }
        return hoaDon;
    }

    private async Task<decimal> TinhGiaTriTraAsync(HoaDon hoaDon, PhieuTra phieu, CancellationToken ct)
    {
        var dongGoc = hoaDon.DongBans.ToDictionary(dong => dong.MaDongBan, StringComparer.Ordinal);
        decimal tong = 0;
        foreach (var nhom in phieu.DongTras.GroupBy(dong => dong.MaDongBan, StringComparer.Ordinal))
        {
            if (!dongGoc.TryGetValue(nhom.Key, out var ban))
            {
                throw new BusinessRuleViolationException("Dòng trả không thuộc hóa đơn gốc.");
            }
            var traLanNay = nhom.Sum(dong => (long)dong.SoLuongTra);
            var daTra = await traRepo.TinhLuongDaTraAsync(ban.MaDongBan, ct);
            if (traLanNay + daTra > ban.SoLuongBan)
            {
                throw new BusinessRuleViolationException($"Số lượng trả vượt số lượng còn lại của dòng {ban.MaDongBan}.");
            }
            tong += traLanNay * ban.DonGiaThucBan;
        }
        return tong;
    }
}
