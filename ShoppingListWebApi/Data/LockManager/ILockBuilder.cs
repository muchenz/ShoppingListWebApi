using System;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Data.LockManager;
public interface ILockBuilder
{
    LockManagerPriorityQueue.LockBuilder AddLock(string lockName);
    LockManagerPriorityQueue.LockBuilder AddLockListAggr(int listAggrId);
    LockManagerPriorityQueue.LockBuilder AddLockUser(int userId);
    Task<IAsyncDisposable> LockAsync();
}