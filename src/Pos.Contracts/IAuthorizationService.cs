namespace Pos.Contracts;

public interface IAuthorizationService
{
    Task KiemTraQuyenAsync(string maTaiKhoan, string maQuyen, CancellationToken ct);
    Task<IReadOnlySet<string>> LayTapQuyenAsync(string maTaiKhoan, CancellationToken ct);

    async Task<bool> CoQuyenAsync(string maTaiKhoan, string maQuyen, CancellationToken ct)
        => (await LayTapQuyenAsync(maTaiKhoan, ct)).Contains(maQuyen);
}
