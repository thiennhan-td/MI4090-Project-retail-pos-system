using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;
using Pos.Infrastructure.Persistence;
using static Pos.Infrastructure.Repositories.Counter.CounterSql;

namespace Pos.Infrastructure.Repositories.Counter;

public sealed class SqlCounterRequestRepository(SqlUnitOfWork work) : ICounterRequestRepository
{
    public async Task<KetQuaYeuCauQuay?> KhoaVaDocAsync(string khoa, CancellationToken ct)
    {
        await using var khoaLenh = work.Lenh("""
            DECLARE @ketQua int;
            EXEC @ketQua = sys.sp_getapplock @Resource=@khoa, @LockMode='Exclusive',
                @LockOwner='Transaction', @LockTimeout=10000;
            SELECT @ketQua;
            """, Chu("@khoa", $"POS:Quay:{khoa}", 255));
        if ((int)(await khoaLenh.ExecuteScalarAsync(ct))! < 0)
        {
            throw new ConcurrencyConflictException("Không thể khóa yêu cầu. Hãy gửi lại với cùng mã yêu cầu.");
        }

        await using var lenh = work.Lenh("""
            SELECT dauVanTay, noiDungKetQua FROM dbo.YeuCauGhiQuay WHERE khoa=@khoa;
            """, Chu("@khoa", khoa, 64));
        await using var r = await lenh.ExecuteReaderAsync(ct);
        return await r.ReadAsync(ct) ? new(r.GetString(0), r.GetString(1)) : null;
    }

    public Task ThemAsync(string khoa, KetQuaYeuCauQuay ketQua, CancellationToken ct) => work.Ghi("""
        INSERT dbo.YeuCauGhiQuay(khoa, dauVanTay, noiDungKetQua) VALUES(@khoa, @dauVanTay, @noiDung);
        """, ct, Chu("@khoa", khoa, 64), Chu("@dauVanTay", ketQua.DauVanTay, 64),
        Chu("@noiDung", ketQua.NoiDungKetQua, -1));
}
