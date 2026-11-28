namespace Pos.Domain.Interfaces;

public interface ICounterAuthorizationRepository
{
    // Trong transaction hiện tại, trả vai trò chỉ khi tài khoản đang hoạt động và vai trò có quyền.
    Task<string?> LayVaiTroCoQuyenAsync(string maTaiKhoan, string maQuyen, CancellationToken ct);
}
