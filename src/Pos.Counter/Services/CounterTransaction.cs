using Pos.Domain.Constants;
using Pos.Domain.Entities;
using Pos.Domain.Exceptions;
using Pos.Domain.Interfaces;

namespace Pos.Counter.Services;

internal static class CounterTransaction
{
    public static async Task<T> ThucThiAsync<T>(this IUnitOfWork work, Func<Task<T>> action, CancellationToken ct)
    {
        await work.BeginTransactionAsync(ct);
        try
        {
            var result = await action();
            await work.CommitAsync(ct);
            return result;
        }
        catch (Exception original)
        {
            try
            {
                await work.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollback)
            {
                original.Data["Counter.RollbackException"] = rollback;
            }
            throw;
        }
    }

    public static async Task<CaBan> KhoaCaMoAsync(this ICaBanRepository repo, string maCa, CancellationToken ct)
    {
        var ca = await repo.KhoaTheoMaAsync(maCa, ct)
            ?? throw new EntityNotFoundException("Không tìm thấy ca bán.");
        if (ca.TrangThai != TrangThaiChungTu.DangMo)
        {
            throw new InvalidStateTransitionException("Ca bán đã đóng.");
        }
        return ca;
    }

    public static void KiemTraChuCa(CaBan ca, string actor)
    {
        if (ca.MaNguoiMo != actor)
        {
            throw new BusinessRuleViolationException("Ca bán không thuộc tài khoản thực hiện.");
        }
    }
}
