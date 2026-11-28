namespace Pos.Domain.Exceptions;

public sealed class UnauthorizedActionException(string maQuyen)
    : PosDomainException($"Tài khoản thiếu quyền {maQuyen}.")
{
    public string MaQuyen { get; } = maQuyen;
}
