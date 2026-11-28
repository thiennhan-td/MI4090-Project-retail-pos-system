using System.Data;
using Microsoft.Data.SqlClient;
using Pos.Domain.Exceptions;
using Pos.Infrastructure.Persistence;

namespace Pos.Infrastructure.Repositories.Counter;

internal static class CounterSql
{
    public static SqlParameter Ma(string name, string? value) => new(name, SqlDbType.VarChar, 32)
        { Value = (object?)value ?? DBNull.Value };
    public static SqlParameter Chu(string name, string? value, int size) => new(name, SqlDbType.NVarChar, size)
        { Value = (object?)value ?? DBNull.Value };
    public static SqlParameter Tien(string name, decimal? value) => new(name, SqlDbType.Decimal)
        { Precision = 19, Scale = 0, Value = (object?)value ?? DBNull.Value };
    public static SqlParameter Luc(string name, DateTime? value) => new(name, SqlDbType.DateTime2)
        { Scale = 3, Value = (object?)value ?? DBNull.Value };
    public static SqlParameter So(string name, int value) => new(name, SqlDbType.Int) { Value = value };

    public static SqlCommand Lenh(this SqlUnitOfWork work, string sql, params SqlParameter[] parameters)
    {
        var command = work.CreateCommand(sql);
        command.Parameters.AddRange(parameters);
        return command;
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
            throw new ConcurrencyConflictException("Chứng từ đã thay đổi hoặc không còn tồn tại.");
        }
    }

    public static DateTime Utc(this SqlDataReader reader, int i)
        => DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Utc);
    public static DateTime? LucNull(this SqlDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.Utc(i);
    public static string? ChuNull(this SqlDataReader reader, int i)
        => reader.IsDBNull(i) ? null : reader.GetString(i);
}
