using System;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Data.LockManager;
public interface ILockBuilder
{
    ILockBuilder AddLock(string lockName);
    ILockBuilder AddLockListAggr(int listAggrId);
    ILockBuilder AddLockUser(int userId);
    Task<IAsyncDisposable> LockAsync();
}