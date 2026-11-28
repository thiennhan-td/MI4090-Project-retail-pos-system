using Pos.Contracts;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;

namespace Pos.Inventory.Services;

public sealed class AuthorizationService(ITaiKhoanRepository repository) : IAuthorizationService
{
    public Task<IReadOnlySet<string>> LayTapQuyenAsync(string maTaiKhoan, CancellationToken ct)
        => repository.LayTapQuyenAsync(maTaiKhoan, ct);

    public async Task KiemTraQuyenAsync(string maTaiKhoan, string maQuyen, CancellationToken ct)
    {
        var quyen = await LayTapQuyenAsync(maTaiKhoan, ct);
        if (!quyen.Contains(maQuyen))
        {
            throw new UnauthorizedActionException(maQuyen);
        }
    }
}
