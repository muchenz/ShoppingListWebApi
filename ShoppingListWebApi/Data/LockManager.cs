using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Data;

public class LockManager
{

    private class LockInfo
    {
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    }

    private readonly ConcurrentDictionary<string, LockInfo> _locksDic = new();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _lockTTL = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public LockManager()
    {
        _cleanupInterval = TimeSpan.FromMinutes(5);

        _cleanupTimer = new Timer(_ => Cleanup(), null, _cleanupInterval, _cleanupInterval);
    }

    private readonly SemaphoreSlim _cleanupLock = new SemaphoreSlim(1, 1);

    private void Cleanup()
    {

        if (!_cleanupLock.Wait(0))
        {
            return;
        }
        try
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _locksDic)
            {
                var lockInfo = kvp.Value;
                if (lockInfo.LastUsed + _lockTTL < now &&
                    lockInfo.Semaphore.Wait(0))
                {
                    bool isRemoved = false;
                    try
                    {
                        isRemoved = _locksDic.TryRemove(kvp.Key, out _);
                        if (isRemoved)
                        {
                            lockInfo.Semaphore.Dispose();
                        }
                    }
                    finally
                    {
                        if (!isRemoved)
                        {
                            lockInfo.Semaphore.Release();

                        }
                    }
                }
            }
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    public LockBuilder GetBuilder()
    {
        return new LockBuilder(this);
    }

    public class LockBuilder
    {
        private readonly LockManager _lockManager;
        private readonly List<string> _keys = new();

        public LockBuilder(LockManager lockManager)
        {
            _lockManager = lockManager;
        }

        public LockBuilder AddLockListAggr(int listAggrId)
        {
            _keys.Add($"listaggr:{listAggrId}");
            return this;
        }

        public LockBuilder AddLockUser(int userId)
        {
            _keys.Add($"user:{userId}");
            return this;
        }

        public async Task<IDisposable> LockAsync()
        {
            var keys = _keys.OrderBy(k => k).ToList();

            var lockInfos = new List<LockInfo>();

            foreach (var key in keys)
            {
                var lockInfo = _lockManager._locksDic.GetOrAdd(key, _ => new LockInfo());

                await lockInfo.Semaphore.WaitAsync();
                lockInfos.Add(lockInfo);

            }

            return new Releaser(lockInfos);

        }

    }

    private class Releaser : IDisposable
    {
        private readonly List<LockInfo> _lockInfos;
        private bool _disposed = false;

        public Releaser(List<LockInfo> lockInfos)
        {
            _lockInfos = lockInfos;
        }


        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var lockInfo in _lockInfos)
                {
                    lockInfo.Semaphore.Release();
                    lockInfo.LastUsed = DateTime.UtcNow;
                }

                _disposed = true;
            }
        }
    }

}

public class LockManagerPriorityQueue
{

    private class LockInfo
    {
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    }

    private readonly ConcurrentDictionary<string, LockInfo> _locksDic = new();
    private readonly PriorityQueue<string, long> _expiryQueue = new();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _lockTTL = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _queueLock = new SemaphoreSlim(1, 1);
    public LockManagerPriorityQueue()
    {
        _cleanupInterval = TimeSpan.FromMinutes(5);

        _cleanupTimer = new Timer(async _ => await Cleanup(), null, _cleanupInterval, _cleanupInterval);
    }

    private readonly SemaphoreSlim _cleanupLock = new SemaphoreSlim(1, 1);

    private async Task Cleanup()
    {
        if (!_cleanupLock.Wait(0))
        {
            return;
        }
        await _queueLock.WaitAsync();

        try
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var now = DateTime.UtcNow;
            while (_expiryQueue.TryPeek(out _, out var priorityTicks) && priorityTicks + _lockTTL.Ticks < nowTicks)
            {

                if (_expiryQueue.TryDequeue(out var key, out _) &&
                    _locksDic.TryGetValue(key, out LockInfo lockInfo))
                {
                    if (lockInfo.LastUsed + _lockTTL < now
                        && lockInfo.Semaphore.Wait(0))
                    {
                        bool isRemoved = false;
                        try
                        {
                            isRemoved = _locksDic.TryRemove(key, out _);
                            if (isRemoved)
                            {
                                lockInfo.Semaphore.Dispose();
                            }
                        }
                        finally
                        {
                            if (!isRemoved)
                            {
                                lockInfo.Semaphore.Release();

                            }
                        }
                    }
                }
            }
        }
        finally
        {
            _cleanupLock.Release();
            _queueLock.Release();
        }
    }

    public LockBuilder GetBuilder()
    {
        return new LockBuilder(this);
    }

    public class LockBuilder
    {
        private readonly LockManagerPriorityQueue _lockManager;
        private readonly List<string> _keys = new();

        public LockBuilder(LockManagerPriorityQueue lockManager)
        {
            _lockManager = lockManager;
        }

        public LockBuilder AddLockListAggr(int listAggrId)
        {
            _keys.Add($"listaggr:{listAggrId}");
            return this;
        }

        public LockBuilder AddLockUser(int userId)
        {
            _keys.Add($"user:{userId}");
            return this;
        }

        public async Task<IAsyncDisposable> LockAsync()
        {
            var keys = _keys.OrderBy(k => k).ToList();

            var lockInfoList = new List<(string key, LockInfo lockInfo)>();

            await _lockManager._queueLock.WaitAsync();
            foreach (var key in keys)
            {

                var lockInfo = _lockManager._locksDic.GetOrAdd(key, _ => new LockInfo());

                await lockInfo.Semaphore.WaitAsync();
                lockInfoList.Add((key, lockInfo));
                //  _lockManager._expiryQueue.Enqueue(key, lockInfo.LastUsed.Ticks); 



            }
            _lockManager._queueLock.Release();
            return new Releaser(lockInfoList, _lockManager);

        }

    }

    private class Releaser : IAsyncDisposable
    {
        private readonly List<(string key, LockInfo lockInfo)> _lockInfoList;
        private readonly LockManagerPriorityQueue _lockManager;
        private bool _disposed = false;

        public Releaser(List<(string key, LockInfo lockInfo)> lockInfoList, LockManagerPriorityQueue lockManager)
        {
            _lockInfoList = lockInfoList;
            _lockManager = lockManager;
        }



        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await _lockManager._queueLock.WaitAsync();
                try
                {
                    foreach (var item in _lockInfoList)
                    {
                        item.lockInfo.Semaphore.Release();
                        item.lockInfo.LastUsed = DateTime.UtcNow;
                        _lockManager._expiryQueue.Enqueue(item.key, item.lockInfo.LastUsed.Ticks);
                    }
                }
                finally
                {
                    _lockManager._queueLock.Release();

                    _disposed = true;
                }
            }
        }
    }

}