using System.Data;
using Microsoft.Data.SqlClient;
using Pos.Domain.Exceptions;
using Pos.Infrastructure.Persistence;

namespace Pos.Infrastructure.Repositories.Inventory;

internal static class InventorySql
{
    public static SqlParameter Ma(string name, string? value)
    {
        // SqlParameter.Size can truncate an oversized code before SQL compares it.
        if (value is not null && (value.Length > 32 || value.Any(kyTu => kyTu is <= ' ' or > '~')))
        {
            throw new BusinessRuleViolationException("Mã tham chiếu không hợp lệ.");
        }

        return new(name, SqlDbType.VarChar, 32) { Value = (object?)value ?? DBNull.Value };
    }
    public static SqlParameter Chu(string name, string? value, int size) => new(name, SqlDbType.NVarChar, size)
        { Value = (object?)value ?? DBNull.Value };
    public static SqlParameter Tien(string name, decimal? value) => new(name, SqlDbType.Decimal)
        { Precision = 19, Scale = 0, Value = (object?)value ?? DBNull.Value };
    public static SqlParameter Luc(string name, DateTime? value) => new(name, SqlDbType.DateTime2)
        { Scale = 3, Value = (object?)value ?? DBNull.Value };
    public static SqlParameter Ngay(string name, DateOnly? value) => new(name, SqlDbType.Date)
        { Value = (object?)value?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value };
    public static SqlParameter So(string name, int? value) => new(name, SqlDbType.Int)
        { Value = (object?)value ?? DBNull.Value };

    public static SqlCommand Lenh(this SqlUnitOfWork work, string sql, params SqlParameter[] parameters)
    {
        var command = work.CreateCommand(sql);
        command.Parameters.AddRange(parameters);
        return command;
    }

    public static async Task<T> DocAsync<T>(this SqlUnitOfWork work, SqlConnectionFactory factory,
        string sql, Func<SqlCommand, CancellationToken, Task<T>> doc, CancellationToken ct,
        params SqlParameter[] parameters)
    {
        if (work.CoGiaoDich)
        {
            await using var command = work.Lenh(sql, parameters);
            return await doc(command, ct);
        }

        await using var connection = factory.CreateConnection();
        await connection.OpenAsync(ct);
        await using var readCommand = new SqlCommand(sql, connection);
        readCommand.Parameters.AddRange(parameters);
        return await doc(readCommand, ct);
    }

    public static async Task<string> SinhMa(this SqlUnitOfWork work, string sql, CancellationToken ct)
    {
        await using var command = work.CreateCommand(sql);
        return (string)(await command.ExecuteScalarAsync(ct))!;
    }

    public static async Task Ghi(this SqlUnitOfWork work, string sql, CancellationToken ct,
        params SqlParameter[] parameters)
    {
        await using var command = work.Lenh(sql, parameters);
        await command.ExecuteNonQueryAsync(ct);
    }

    public static async Task SuaMot(this SqlUnitOfWork work, string sql, CancellationToken ct,
        params SqlParameter[] parameters)
    {
        await using var command = work.Lenh(sql, parameters);
        if (await command.ExecuteNonQueryAsync(ct) != 1)
        {
            throw new ConcurrencyConflictException("Dữ liệu đã thay đổi hoặc không còn tồn tại.");
        }
    }

    public static async Task<T?> DocMotAsync<T>(SqlCommand command, Func<SqlDataReader, T> anhXa,
        CancellationToken ct) where T : class
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? anhXa(reader) : null;
    }

    public static async Task<IReadOnlyList<T>> DocNhieuAsync<T>(SqlCommand command, Func<SqlDataReader, T> anhXa,
        CancellationToken ct)
    {
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<T>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(anhXa(reader));
        }

        return result;
    }

    public static DateTime Utc(this SqlDataReader reader, int i)
        => DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Utc);
    public static DateTime? LucNull(this SqlDataReader reader, int i) => reader.IsDBNull(i) ? null : reader.Utc(i);
    public static string? ChuNull(this SqlDataReader reader, int i) => reader.IsDBNull(i) ? null : reader.GetString(i);

    public static async Task ChanTrungAsync(Func<Task> action, string message)
    {
        try
        {
            await action();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new DuplicateRequestException(message);
        }
    }
}
