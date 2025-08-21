using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Data;

public class HierarchicalLockManager2
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public LockBuilder CreateLock() => new LockBuilder(this);

    private SemaphoreSlim GetLock(string key) =>
        _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    public class LockBuilder
    {
        private readonly HierarchicalLockManager2 _manager;
        private readonly List<string> _keys = new();

        internal LockBuilder(HierarchicalLockManager2 manager)
        {
            _manager = manager;
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
            // sortujemy klucze → zawsze ta sama kolejność
            var orderedKeys = _keys.OrderBy(k => k).ToList();

            var acquired = new List<(string key, SemaphoreSlim sem)>();

            try
            {
                foreach (var key in orderedKeys)
                {
                    var sem = _manager.GetLock(key);
                    await sem.WaitAsync();
                    acquired.Add((key, sem));
                }

                return new Releaser(acquired);
            }
            catch
            {
                foreach (var (key, sem) in acquired)
                {
                    sem.Release();
                }

                throw;
            }
        }

        private class Releaser : IDisposable
        {
            private readonly List<(string key, SemaphoreSlim sem)> _acquired;

            public Releaser(List<(string key, SemaphoreSlim sem)> acquired)
            {
                _acquired = acquired;
            }

            public void Dispose()
            {
                foreach (var (key, sem) in _acquired)
                {
                    sem.Release();
                }
            }
        }
    }
}

public class ResourceLockManager<TKey>
{
    private class LockInfo
    {
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    }

    private readonly ConcurrentDictionary<TKey, LockInfo> _locks = new();
    private readonly TimeSpan _lockTtl;
    private readonly Timer _cleanupTimer;

    public ResourceLockManager(TimeSpan lockTtl, TimeSpan cleanupInterval)
    {
        _lockTtl = lockTtl;
        _cleanupTimer = new Timer(_ => Cleanup(), null, cleanupInterval, cleanupInterval);
    }

    public async Task<IDisposable> AcquireAsync(TKey key)
    {
        var lockInfo = _locks.GetOrAdd(key, _ => new LockInfo());

        await lockInfo.Semaphore.WaitAsync();
        lockInfo.LastUsed = DateTime.UtcNow;

        return new Releaser(this, key, lockInfo);
    }

    private void Release(TKey key, LockInfo lockInfo)
    {
        lockInfo.LastUsed = DateTime.UtcNow;
        lockInfo.Semaphore.Release();
    }

    private void Cleanup()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _locks)
        {
            var lockInfo = kvp.Value;
            if (!lockInfo.Semaphore.CurrentCount.Equals(0) && (now - lockInfo.LastUsed) > _lockTtl)
            {
                _locks.TryRemove(kvp.Key, out _);
            }
        }
    }

    private class Releaser : IDisposable
    {
        private readonly ResourceLockManager<TKey> _manager;
        private readonly TKey _key;
        private readonly LockInfo _lockInfo;
        private bool _disposed;

        public Releaser(ResourceLockManager<TKey> manager, TKey key, LockInfo lockInfo)
        {
            _manager = manager;
            _key = key;
            _lockInfo = lockInfo;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _manager.Release(_key, _lockInfo);
                _disposed = true;
            }
        }
    }
}

public class HierarchicalLockManager
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

    public HierarchicalLockManager()
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
        private readonly HierarchicalLockManager _lockManager;
        private readonly List<string> _keys = new();

        public LockBuilder(HierarchicalLockManager lockManager)
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