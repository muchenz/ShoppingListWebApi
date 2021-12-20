using Microsoft.Extensions.DependencyInjection;
using Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseDatabase
{
    public static class ExtensionFD
    {
        public static void AddFirebasedDatabase(this IServiceCollection services)
        {
            services.AddTransient<UserEndpointFD, UserEndpointFD>();
            services.AddTransient<ListAggregatorEndpointFD, ListAggregatorEndpointFD>();
            services.AddTransient<ListItemEndpointFD, ListItemEndpointFD>();
            services.AddTransient<InvitationEndpointFD, InvitationEndpointFD>();
            services.AddTransient<ListEndpointFD, ListEndpointFD>();

        }

    }
}
