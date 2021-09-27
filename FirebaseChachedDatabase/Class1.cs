using EFDataBase;
using FirebaseDatabase;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Shared;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FirebaseChachedDatabase
{




    public static class Class1
    {

        public static Task SetAsync<T, TKey>(this IDistributedCache cache,TKey anyKey, T value)
         where T : class
        {
            var key = anyKey switch
            {
                string k => k,
                _ => anyKey.ToString(),
            };
            var data = JsonSerializer.Serialize(value);

            return cache.SetStringAsync(key,data);
        }


        public static async Task<GetCachedValue<T>> GetOrAddAsync<T, TKey>(
            this IDistributedCache cache,
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

            var value = await cache.GetAsync<T>(key);
            if (value == null)
            {
                value = await factory(anyKey);
                await cache.SetStringAsync(key, JsonSerializer.Serialize(value));
                return new GetCachedValue<T> { Cached = false, value = value };
            }

            return new GetCachedValue<T> { Cached = true, value = value };
        }

        public static async Task<T> GetAsync<T>(this IDistributedCache cache, string key)
            where T : class
        {
            var jsonValue = await cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(jsonValue))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(jsonValue);
        }

        public static void AddFirebaseCaschedDatabas (this IServiceCollection services)
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
        public T value { get; set; }

    }

}
