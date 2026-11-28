using System.Data;
using Microsoft.Data.SqlClient;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Counter.CounterSql;

namespace Pos.Infrastructure.Repositories.Counter;

public sealed class SqlCounterAuthorizationRepository(SqlUnitOfWork work) : ICounterAuthorizationRepository
{
    public async Task<string?> LayVaiTroCoQuyenAsync(string maTaiKhoan, string maQuyen, CancellationToken ct)
    {
        // Giữ khóa tới cuối giao dịch để thay đổi tài khoản hoặc quyền không xen vào quyết định ghi.
        await using var command = work.Lenh("""
            SELECT tk.maVaiTro
            FROM dbo.TaiKhoan AS tk WITH (HOLDLOCK)
            INNER JOIN dbo.QuyenVaiTro AS qvt WITH (HOLDLOCK) ON qvt.maVaiTro = tk.maVaiTro
            WHERE tk.maTaiKhoan = @taiKhoan AND tk.trangThai = 'HoatDong' AND qvt.maQuyen = @quyen;
            """, Ma("@taiKhoan", maTaiKhoan), new SqlParameter("@quyen", SqlDbType.VarChar, 64) { Value = maQuyen });
        return (string?)await command.ExecuteScalarAsync(ct);
    }
}
