namespace Pos.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    /// <summary>Hoàn tác và giải phóng giao dịch. Khi xử lý lỗi, gọi với CancellationToken.None.</summary>
    Task RollbackAsync(CancellationToken ct = default);
}
