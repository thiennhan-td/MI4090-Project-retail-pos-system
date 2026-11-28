using System.Data;
using Microsoft.Data.SqlClient;
using Pos.Domain.Interfaces;

namespace Pos.Infrastructure.Persistence;

// Dùng một instance cho mỗi phạm vi nghiệp vụ. Thực thi lệnh tuần tự trên cùng transaction.
public sealed class SqlUnitOfWork(SqlConnectionFactory factory) : IUnitOfWork
{
    private SqlConnection? _connection;
    private SqlTransaction? _transaction;
    private bool _disposed;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_transaction is not null)
        {
            throw new InvalidOperationException("Một giao dịch đang được thực hiện.");
        }

        var connection = factory.CreateConnection();
        try
        {
            await connection.OpenAsync(ct);
            _transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            _connection = connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public SqlCommand CreateCommand(string commandText)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var transaction = _transaction
            ?? throw new InvalidOperationException("Cần mở giao dịch trước khi tạo lệnh.");
        return new SqlCommand(commandText, _connection, transaction);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var transaction = _transaction
            ?? throw new InvalidOperationException("Không có giao dịch để commit.");
        // Khi commit lỗi, giữ transaction để bên điều phối có thể rollback.
        await transaction.CommitAsync(ct);
        await ReleaseAsync();
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_transaction is not { } transaction)
        {
            return;
        }

        try
        {
            if (transaction.Connection is not null)
            {
                await transaction.RollbackAsync(ct);
            }
        }
        finally
        {
            await ReleaseAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            // Kết thúc giao dịch kể cả khi token của nghiệp vụ đã bị hủy.
            await RollbackAsync(CancellationToken.None);
        }
        finally
        {
            _disposed = true;
        }
    }

    private async Task ReleaseAsync()
    {
        var transaction = _transaction;
        var connection = _connection;
        _transaction = null;
        _connection = null;
        try
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
        finally
        {
            if (connection is not null)
            {
                await connection.DisposeAsync();
            }
        }
    }
}
