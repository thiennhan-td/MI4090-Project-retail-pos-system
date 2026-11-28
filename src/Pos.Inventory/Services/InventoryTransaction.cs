using Pos.Domain.Interfaces;

namespace Pos.Inventory.Services;

internal static class InventoryTransaction
{
    public static Task ChayAsync(IUnitOfWork work, Func<Task> action, CancellationToken ct)
        => ChayAsync(work, async () =>
        {
            await action();
            return true;
        }, ct);

    public static async Task<T> ChayAsync<T>(IUnitOfWork work, Func<Task<T>> action, CancellationToken ct)
    {
        await work.BeginTransactionAsync(ct);
        try
        {
            var result = await action();
            await work.CommitAsync(ct);
            return result;
        }
        catch (Exception error)
        {
            try
            {
                await work.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackError)
            {
                error.Data["RollbackException"] = rollbackError;
            }

            throw;
        }
    }
}
