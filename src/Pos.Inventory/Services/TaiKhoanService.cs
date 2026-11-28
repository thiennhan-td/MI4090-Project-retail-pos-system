using System.Security.Cryptography;
using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;

namespace Pos.Inventory.Services;

public sealed record TaiKhoanPosDto(string MaTaiKhoan, string HoTen, string TenDangNhap,
    string MaVaiTro, bool HoatDong, string PhienBan);

public sealed record LuuTaiKhoanDto(string? MaTaiKhoan, string HoTen, string TenDangNhap,
    string MaVaiTro, string? MatKhau, bool HoatDong, string? PhienBan = null);

public sealed record PhanQuyenPosDto(IReadOnlyList<VaiTro> VaiTro, IReadOnlyList<Quyen> DanhSachQuyen,
    IReadOnlySet<string> QuyenDaGan);

public sealed class TaiKhoanService(IQuanTriTaiKhoanRepository repository, IUnitOfWork work,
    IAuthorizationService authorization, IMatKhauHasher hasher)
{
    private const string QuanLy = "QuanLy";

    public async Task<IReadOnlyList<TaiKhoanPosDto>> TimAsync(string maNguoiThaoTac, string? tuKhoa,
        CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.HT_QL_TAI_KHOAN, ct);
        var danhSach = await repository.TimTaiKhoanAsync(tuKhoa?.Trim(), ct);
        return danhSach.Select(Dto).ToArray();
    }

    public async Task<TaiKhoanPosDto> LuuAsync(string maNguoiThaoTac, LuuTaiKhoanDto yeuCau, CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.HT_QL_TAI_KHOAN, ct);
        if (yeuCau.MaTaiKhoan is null && string.IsNullOrWhiteSpace(yeuCau.MatKhau))
        {
            throw new BusinessRuleViolationException("Cần mật khẩu để tạo tài khoản.");
        }

        var tenDangNhap = yeuCau.TenDangNhap.Trim();
        var bam = string.IsNullOrWhiteSpace(yeuCau.MatKhau) ? null : hasher.Bam(yeuCau.MatKhau);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            // Mọi đổi vai trò/trạng thái dùng cùng khóa để giữ lại ít nhất một quản lý.
            await KiemTraVaiTroAsync(QuanLy, ct);
            await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.HT_QL_TAI_KHOAN, ct);
            if (yeuCau.MaVaiTro != QuanLy)
            {
                await KiemTraVaiTroAsync(yeuCau.MaVaiTro, ct);
            }

            var cu = yeuCau.MaTaiKhoan is null ? null
                : await repository.LayTheoMaAsync(yeuCau.MaTaiKhoan, ct)
                    ?? throw new EntityNotFoundException("Không tìm thấy tài khoản.");

            if (cu is not null && cu.MaTaiKhoan != yeuCau.MaTaiKhoan)
            {
                throw new EntityNotFoundException("Không tìm thấy tài khoản.");
            }

            if (cu is not null && yeuCau.PhienBan != TaoPhienBan(cu))
            {
                throw new ConcurrencyConflictException("Tài khoản đã thay đổi. Hãy tải lại tài khoản.");
            }

            if (cu is null || cu.MaVaiTro != yeuCau.MaVaiTro)
            {
                await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.HT_PHAN_QUYEN, ct);
            }

            if (cu is { MaVaiTro: QuanLy, TrangThai: TrangThaiChungTu.HoatDong }
                && (!yeuCau.HoatDong || yeuCau.MaVaiTro != QuanLy)
                && await repository.DemQuanLyKhacDangHoatDongAsync(cu.MaTaiKhoan, ct) == 0)
            {
                throw new BusinessRuleViolationException("Cần giữ ít nhất một tài khoản Quản lý hoạt động.");
            }

            if (cu is not null && cu.MaVaiTro == yeuCau.MaVaiTro
                && (bam is not null || tenDangNhap != cu.TenDangNhap
                    || yeuCau.HoatDong != (cu.TrangThai == TrangThaiChungTu.HoatDong)))
            {
                var quyenDich = await repository.LayQuyenVaiTroAsync(cu.MaVaiTro, ct);
                var quyenNguoiThaoTac = await authorization.LayTapQuyenAsync(maNguoiThaoTac, ct);
                if (!quyenDich.IsSubsetOf(quyenNguoiThaoTac))
                {
                    await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.HT_PHAN_QUYEN, ct);
                }
            }

            var ma = cu?.MaTaiKhoan ?? await repository.SinhMaTaiKhoanAsync(ct);
            var taiKhoan = new TaiKhoan(ma, yeuCau.MaVaiTro, yeuCau.HoTen.Trim(), tenDangNhap, bam ?? cu!.MatKhauBam);
            if (!yeuCau.HoatDong)
            {
                taiKhoan.Khoa();
            }

            if (cu is null)
            {
                await repository.ThemAsync(taiKhoan, ct);
            }
            else
            {
                await repository.CapNhatAsync(taiKhoan, ct);
            }

            return Dto(taiKhoan);
        }, ct);
    }

    public async Task<PhanQuyenPosDto> LayPhanQuyenAsync(string maNguoiThaoTac, string maVaiTro, CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.HT_PHAN_QUYEN, ct);
        var vaiTro = await repository.LayVaiTroAsync(ct);
        if (!vaiTro.Any(x => x.MaVaiTro == maVaiTro))
        {
            throw new EntityNotFoundException("Không tìm thấy vai trò.");
        }

        return new(vaiTro, await repository.LayDanhSachQuyenAsync(ct),
            await repository.LayQuyenVaiTroAsync(maVaiTro, ct));
    }

    public async Task CapNhatQuyenAsync(string maNguoiThaoTac, string maVaiTro,
        IEnumerable<string> maQuyen, CancellationToken ct)
    {
        await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.HT_PHAN_QUYEN, ct);
        var tapQuyen = maQuyen.ToHashSet(StringComparer.Ordinal);
        await InventoryTransaction.ChayAsync(work, async () =>
        {
            await KiemTraVaiTroAsync(QuanLy, ct);
            await authorization.KiemTraQuyenAsync(maNguoiThaoTac, MaQuyenPos.HT_PHAN_QUYEN, ct);
            if (maVaiTro != QuanLy)
            {
                await KiemTraVaiTroAsync(maVaiTro, ct);
            }

            if (maVaiTro == QuanLy && (!tapQuyen.Contains(MaQuyenPos.HT_PHAN_QUYEN)
                || !tapQuyen.Contains(MaQuyenPos.HT_QL_TAI_KHOAN)))
            {
                throw new BusinessRuleViolationException("Vai trò Quản lý cần giữ quyền quản lý tài khoản và phân quyền.");
            }

            var hopLe = (await repository.LayDanhSachQuyenAsync(ct)).Select(x => x.MaQuyen);
            if (tapQuyen.Except(hopLe, StringComparer.Ordinal).Any())
            {
                throw new BusinessRuleViolationException("Mã quyền không hợp lệ.");
            }

            await repository.ThayQuyenVaiTroAsync(maVaiTro, tapQuyen, ct);
        }, ct);
    }

    private async Task KiemTraVaiTroAsync(string maVaiTro, CancellationToken ct)
    {
        var vaiTro = await repository.KhoaVaiTroAsync(maVaiTro, ct);
        if (vaiTro?.MaVaiTro != maVaiTro)
        {
            throw new EntityNotFoundException("Không tìm thấy vai trò.");
        }
    }

    private static string TaoPhienBan(TaiKhoan taiKhoan)
    {
        using var duLieu = new MemoryStream();
        using var ghi = new BinaryWriter(duLieu);
        ghi.Write(taiKhoan.MaTaiKhoan);
        ghi.Write(taiKhoan.MaVaiTro);
        ghi.Write(taiKhoan.HoTen);
        ghi.Write(taiKhoan.TenDangNhap);
        ghi.Write(taiKhoan.TrangThai);
        ghi.Write(taiKhoan.MatKhauBam);
        ghi.Flush();
        return Convert.ToHexString(SHA256.HashData(duLieu.GetBuffer().AsSpan(0, checked((int)duLieu.Length))));
    }

    private static TaiKhoanPosDto Dto(TaiKhoan entity) => new(entity.MaTaiKhoan, entity.HoTen,
        entity.TenDangNhap, entity.MaVaiTro, entity.TrangThai == TrangThaiChungTu.HoatDong, TaoPhienBan(entity));
}
