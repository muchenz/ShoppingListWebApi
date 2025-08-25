using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Data;

public class LockManager : IDisposable
{

    private class LockInfo
    {
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        public int InUseCount = 0;
    }

    private readonly ConcurrentDictionary<string, LockInfo> _locksDic = new();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _lockTTL = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public LockManager()
    {
        _cleanupInterval = TimeSpan.FromMinutes(5);

        _cleanupTimer = new Timer(async _ => await Cleanup(), null, _cleanupInterval, _cleanupInterval);
    }

    private readonly SemaphoreSlim _cleanupLock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);

    private async Task Cleanup()
    {

        if (!_cleanupLock.Wait(0))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var tempKeyList = _locksDic.Where(kvp => kvp.Value.LastUsed + _lockTTL < now).Select(a => a.Key).ToList();

        await _stateLock.WaitAsync();
        try
        {
            now = DateTime.UtcNow;
            foreach (var key in tempKeyList)
            {
                if (_locksDic.TryGetValue(key, out var lockInfo))
                {
                    if (lockInfo.LastUsed + _lockTTL < now &&
                        lockInfo.InUseCount == 0)
                    {
                        if (_locksDic.TryRemove(key, out _))
                        {
                            lockInfo.Semaphore.Dispose();
                        }
                    }
                }
            }
        }
        finally
        {
            _stateLock.Release();
            _cleanupLock.Release();
        }
    }

    public LockBuilder GetBuilder()
    {
        return new LockBuilder(this);
    }



    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _cleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _cleanupTimer.Dispose();

        //_cleanupLock.Wait();
        //try
        //{
        //    _stateLock.Wait();
        //    try
        //    {
        //        foreach (var kvp in _locksDic)
        //        {
        //            kvp.Value.Semaphore.Dispose();
        //        }
        //        _locksDic.Clear();
        //    }
        //    finally
        //    {
        //        _stateLock.Release();
        //    }
        //}
        //finally
        //{
        //    _cleanupLock.Release();
        //    _cleanupLock.Dispose();
        //    _stateLock.Dispose();
        //}
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
            try
            {
                await _lockManager._stateLock.WaitAsync();

                foreach (var key in keys)
                {
                    var lockInfo = _lockManager._locksDic.GetOrAdd(key, _ => new LockInfo());
                    lockInfo.InUseCount++;
                    lockInfos.Add(lockInfo);
                }
            }
            finally
            {
                _lockManager._stateLock.Release();
                foreach (var lockInfo in lockInfos)
                {
                    await lockInfo.Semaphore.WaitAsync();
                }

            }
            return new Releaser(_lockManager, lockInfos);

        }

    }


    private class Releaser : IDisposable
    {
        private readonly LockManager _lockManager;
        private readonly List<LockInfo> _lockInfos;
        private bool _disposed = false;

        public Releaser(LockManager lockManager, List<LockInfo> lockInfos)
        {
            _lockManager = lockManager;
            _lockInfos = lockInfos;
        }


        public void Dispose()
        {
            if (_disposed) return;

            _lockManager._stateLock.Wait();
            try
            {
                foreach (var info in _lockInfos)
                {
                    info.LastUsed = DateTime.UtcNow;
                    //Interlocked.Decrement(ref info.InUseCount);
                    info.InUseCount--;
                    info.Semaphore.Release();
                }
                _disposed = true;
            }
            finally
            {
                _lockManager._stateLock.Release();

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

        public int InUseCount { get; set; } = 0;

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
                        && lockInfo.InUseCount == 0)
                    {

                        if (_locksDic.TryRemove(key, out _))
                        {
                            lockInfo.Semaphore.Dispose();
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

                var lockInfo = _lockManager._locksDic.GetOrAdd(key, _ => new LockInfo()); //fresh lockinfo will have lastused now

                lockInfo.InUseCount++;
                //await lockInfo.Semaphore.WaitAsync();
                lockInfoList.Add((key, lockInfo));
                //_lockManager._expiryQueue.Enqueue(key, lockInfo.LastUsed.Ticks); 

            }
            _lockManager._queueLock.Release();

            foreach (var item in lockInfoList)
            {
                await item.lockInfo.Semaphore.WaitAsync();
            }
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
                        item.lockInfo.LastUsed = DateTime.UtcNow;
                        _lockManager._expiryQueue.Enqueue(item.key, item.lockInfo.LastUsed.Ticks);
                        item.lockInfo.InUseCount--;
                        item.lockInfo.Semaphore.Release();
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

public class LockManagerLinkedList
{

    private class LockInfo
    {
        public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
        public DateTime LastUsed { get; set; } = DateTime.UtcNow;
        public int InUseCount { get; set; } = 0;
        public string Key { get; set; } = string.Empty;
    }

    private readonly ConcurrentDictionary<string, LinkedListNode<LockInfo>> _nodeDic = new();
    private readonly LinkedList<LockInfo> _lockList = new();
    private readonly Timer _cleanupTimer;
    private readonly TimeSpan _lockTTL = TimeSpan.FromMinutes(1);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _stateLock = new SemaphoreSlim(1, 1);
    public LockManagerLinkedList()
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
        await _stateLock.WaitAsync();

        try
        {
            var now = DateTime.UtcNow;
            var node = _lockList.First;
            while (node != null)
            {
                var currNode = node;
                node = node.Next;

                if (currNode.Value.InUseCount > 0)
                {
                    continue;
                }

                if (currNode.Value.LastUsed + _lockTTL >= now)
                {
                    break;
                }


                if (_nodeDic.TryRemove(currNode.Value.Key, out _))
                {
                    currNode.Value.Semaphore.Dispose();
                    _lockList.Remove(currNode);
                }

            }
        }
        finally
        {
            _cleanupLock.Release();
            _stateLock.Release();
        }
    }

    public LockBuilder GetBuilder()
    {
        return new LockBuilder(this);
    }

    public class LockBuilder
    {
        private readonly LockManagerLinkedList _lockManager;
        private readonly List<string> _keys = new();

        public LockBuilder(LockManagerLinkedList lockManager)
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

            var lockInfoList = new List<LinkedListNode<LockInfo>>();

            try
            {

                await _lockManager._stateLock.WaitAsync();
                foreach (var key in keys)
                {
                    var isExisting = _lockManager._nodeDic.TryGetValue(key, out var existingNode);

                    if (isExisting)
                    {
                        //_lockManager._lockList.Remove(existingNode);
                        //_lockManager._lockList.AddLast(existingNode);
                        //existingNode.Value.LastUsed = DateTime.UtcNow;
                        existingNode.Value.InUseCount++;

                        lockInfoList.Add(existingNode);
                        //await existingNode.Value.Semaphore.WaitAsync();
                        continue;
                    }

                    var newLockInfo = new LockInfo { Key = key, InUseCount = 1 };
                    var newNode = new LinkedListNode<LockInfo>(newLockInfo);
                    _lockManager._lockList.AddLast(newNode);
                    _lockManager._nodeDic.TryAdd(key, newNode);

                    lockInfoList.Add(newNode);
                    //await existingNode.Value.Semaphore.WaitAsync();

                }
            }
            finally
            {
                _lockManager._stateLock.Release();

                foreach (var node in lockInfoList)
                {
                    await node.Value.Semaphore.WaitAsync();
                }
            }

            return new Releaser(lockInfoList, _lockManager);

        }

    }

    private class Releaser : IAsyncDisposable
    {
        private readonly List<LinkedListNode<LockInfo>> _nodeList;
        private readonly LockManagerLinkedList _lockManager;
        private bool _disposed = false;

        public Releaser(List<LinkedListNode<LockInfo>> nodeList, LockManagerLinkedList lockManager)
        {
            _nodeList = nodeList;
            _lockManager = lockManager;
        }



        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                await _lockManager._stateLock.WaitAsync();
                try
                {
                    foreach (var node in _nodeList)
                    {

                        node.Value.LastUsed = DateTime.UtcNow;

                        _lockManager._lockList.Remove(node);
                        _lockManager._lockList.AddLast(node);
                        node.Value.InUseCount--;
                        node.Value.Semaphore.Release();
                    }
                }
                finally
                {
                    _lockManager._stateLock.Release();
                    _disposed = true;
                }
            }
        }
    }
}
