using Pos.Domain.Constants;
using Pos.Domain.Interfaces;

namespace Pos.Inventory.Services;

public sealed record DanhTinhPosDto(string MaTaiKhoan, string HoTen, string MaVaiTro, IReadOnlySet<string> Quyen);

public sealed class AuthService(IQuanTriTaiKhoanRepository repository, IMatKhauHasher hasher)
{
    public async Task<DanhTinhPosDto?> DangNhapAsync(string tenDangNhap, string matKhau, CancellationToken ct)
    {
        var ten = tenDangNhap.Trim();
        if (ten.Length is 0 or > 64 || string.IsNullOrEmpty(matKhau))
        {
            return null;
        }

        var taiKhoan = await repository.LayTheoTenDangNhapAsync(ten, ct);
        if (taiKhoan is null || taiKhoan.TrangThai != TrangThaiChungTu.HoatDong
            || !hasher.KiemTra(matKhau, taiKhoan.MatKhauBam))
        {
            return null;
        }

        var hoSo = await repository.LayHoSoDangNhapAsync(taiKhoan.MaTaiKhoan, ct);
        if (hoSo is null || hoSo.TaiKhoan.TenDangNhap != taiKhoan.TenDangNhap
            || hoSo.TaiKhoan.MatKhauBam != taiKhoan.MatKhauBam)
        {
            return null;
        }

        var hienTai = hoSo.TaiKhoan;
        return new(hienTai.MaTaiKhoan, hienTai.HoTen, hienTai.MaVaiTro, hoSo.Quyen);
    }
}
