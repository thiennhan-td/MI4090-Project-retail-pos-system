using Pos.Contracts;
using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;

namespace Pos.Inventory.Services;

public sealed record TaiKhoanPosDto(string MaTaiKhoan, string HoTen, string TenDangNhap,
    string MaVaiTro, bool HoatDong);

public sealed record LuuTaiKhoanDto(string? MaTaiKhoan, string HoTen, string TenDangNhap,
    string MaVaiTro, string? MatKhau, bool HoatDong);

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

        var bam = string.IsNullOrWhiteSpace(yeuCau.MatKhau) ? null : hasher.Bam(yeuCau.MatKhau);
        return await InventoryTransaction.ChayAsync(work, async () =>
        {
            // Mọi đổi vai trò/trạng thái dùng cùng khóa để giữ lại ít nhất một quản lý.
            await KiemTraVaiTroAsync(QuanLy, ct);
            if (yeuCau.MaVaiTro != QuanLy)
            {
                await KiemTraVaiTroAsync(yeuCau.MaVaiTro, ct);
            }

            var cu = yeuCau.MaTaiKhoan is null ? null
                : await repository.LayTheoMaAsync(yeuCau.MaTaiKhoan, ct)
                    ?? throw new EntityNotFoundException("Không tìm thấy tài khoản.");

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

            var ma = cu?.MaTaiKhoan ?? await repository.SinhMaTaiKhoanAsync(ct);
            var taiKhoan = new TaiKhoan(ma, yeuCau.MaVaiTro, yeuCau.HoTen.Trim(),
                yeuCau.TenDangNhap.Trim(), bam ?? cu!.MatKhauBam);
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
        if (maVaiTro == QuanLy && (!tapQuyen.Contains(MaQuyenPos.HT_PHAN_QUYEN)
            || !tapQuyen.Contains(MaQuyenPos.HT_QL_TAI_KHOAN)))
        {
            throw new BusinessRuleViolationException("Vai trò Quản lý cần giữ quyền quản lý tài khoản và phân quyền.");
        }

        await InventoryTransaction.ChayAsync(work, async () =>
        {
            await KiemTraVaiTroAsync(maVaiTro, ct);
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
        if (await repository.KhoaVaiTroAsync(maVaiTro, ct) is null)
        {
            throw new EntityNotFoundException("Không tìm thấy vai trò.");
        }
    }

    private static TaiKhoanPosDto Dto(TaiKhoan entity) => new(entity.MaTaiKhoan, entity.HoTen,
        entity.TenDangNhap, entity.MaVaiTro, entity.TrangThai == TrangThaiChungTu.HoatDong);
}
