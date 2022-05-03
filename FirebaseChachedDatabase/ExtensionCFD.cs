using AutoMapper;
using EFDataBase;
using FirebaseDatabase;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Caching.Distributed;

using Microsoft.Extensions.DependencyInjection;
using Shared;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{
    public interface IMiniDistributedCache
    {
        Task RemoveAsync(string key);
        Task SetStringAsync(string key, string value);

        Task<string> GetStringAsync(string key);

    }

    public class FirabaseCache : IMiniDistributedCache
    {
        CollectionReference _cache;
        FirestoreDb Db;
        private readonly IMapper _mapper;

        public FirabaseCache(IMapper mapper)
        {
            Db = FirestoreDb.Create("testnosqldb1");
            _cache = Db.Collection("ShoppibgListCache");
            _mapper = mapper;

        }

        public async Task<string> GetStringAsync(string key)
        {
            var cacheSanp = await _cache.Document(key).GetSnapshotAsync();

            if (!cacheSanp.Exists) return null;

            var cacheString = cacheSanp.ConvertTo<CacheData>();

            return cacheString.JsonString;
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.Document(key).DeleteAsync();
        }

        public async Task SetStringAsync(string key, string value)
        {
            try
            {
                await _cache.Document(key).SetAsync(new CacheData { JsonString=value});
            }
            catch (Exception ex)
            {

                
            }        
           
        }
       
    }
    [FirestoreData]
    public class CacheData
    {
        [FirestoreProperty]
        public string JsonString { get; set; }
    }

    public class DistributedCache : IMiniDistributedCache
    {
        private readonly IDistributedCache _cache;

        public DistributedCache(IDistributedCache cache)
        {
            _cache = cache;
        }
        public Task<string> GetStringAsync(string key)
        {
            return _cache.GetStringAsync(key);
        }

        public Task RemoveAsync(string key)
        {
            return  _cache.RemoveAsync(key);
        }

        public Task SetStringAsync(string key, string value)
        {
            return _cache.SetStringAsync(key, value);
        }
    }


    public class CacheConveinient
    {
        private readonly IMiniDistributedCache _cache;

        public CacheConveinient(IMiniDistributedCache cache)
        {
            _cache = cache;
        }

        public Task RemoveAnyKeyAsync<TKey>(TKey anyKey)
        {
            var key = anyKey switch
            {
                string k => k,
                _ => anyKey.ToString(),
            };


            return _cache.RemoveAsync(key);
        }

        public Task SetAsync<T, TKey>(TKey anyKey, T value)
         where T : class
        {
            var key = anyKey switch
            {
                string k => k,
                _ => anyKey.ToString(),
            };
            var data = JsonSerializer.Serialize(value);

            return _cache.SetStringAsync(key, data);
        }


        public async Task<GetCachedValue<T>> GetOrAddAsync<T, TKey>(
            TKey anyKey,
            Func<TKey, Task<T>> factory
        )
            where T : class
        {
            var key = anyKey switch
            {
                string k => k,
                _ => anyKey.ToString(),
            };

            var value = await GetAsync<T>(key);
            if (value == null)
            {
                value = await factory(anyKey);
                await _cache.SetStringAsync(key, JsonSerializer.Serialize(value));
                return new GetCachedValue<T> { Cached = false, Value = value };
            }

            return new GetCachedValue<T> { Cached = true, Value = value };
        }

        public async Task<GetCachedValue<T>> GetOrAddAsync<T, TKey>(
           TKey anyKey,
           Func<Task<T>> factory
       )
           where T : class
        {
            var key = anyKey switch
            {
                string k => k,
                _ => anyKey.ToString(),
            };

            var value = await GetAsync<T>(key);
            if (value == null)
            {
                value = await factory();
                await _cache.SetStringAsync(key, JsonSerializer.Serialize(value));
                return new GetCachedValue<T> { Cached = false, Value = value };
            }

            return new GetCachedValue<T> { Cached = true, Value = value };
        }

        public async Task UpdateAsync<T, TKey>(
         TKey anyKey,
         Func<T, Task<T>> factory
     )
         where T : class
        {
            var key = anyKey switch
            {
                string k => k,
                _ => anyKey.ToString(),
            };

            var value = await GetAsync<T>(key);
            if (value != null)
            {
                value = await factory(value);
                await _cache.SetStringAsync(key, JsonSerializer.Serialize(value));
            }
        }


        public Task<T> GetAsync<T>(int key)
           where T : class
        {
            return GetAsync<T>(key.ToString());
        }
        public async Task<T> GetAsync<T>(string key)
            where T : class
        {
            var jsonValue = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(jsonValue))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(jsonValue);
        }



    }

    public static class AddFirebaseCaschedDatabasExtensions
    {
        public static void AddFirebaseCaschedDatabas(this IServiceCollection services)
        {
            services.AddTransient<UserEndpointFD, UserEndpointFD>();
            services.AddTransient<ListAggregatorEndpointFD, ListAggregatorEndpointFD>();
            services.AddTransient<ListItemEndpointFD, ListItemEndpointFD>();
            services.AddTransient<InvitationEndpointFD, InvitationEndpointFD>();
            services.AddTransient<ListEndpointFD, ListEndpointFD>();


            services.AddTransient<IUserEndpoint, UserEndpointCFD>();
            services.AddTransient<IListAggregatorEndpoint, ListAggregatorEndpointCFD>();
            services.AddTransient<IListItemEndpoint, ListItemEndpointCFD>();
            services.AddTransient<IInvitationEndpoint, InvitationEndpointCFD>();
            services.AddTransient<IListEndpoint, ListEndpointCFD>();
        }

    }
    public class GetCachedValue<T>
    {

        public bool Cached { get; set; }
        public T Value { get; set; }

    }

}
