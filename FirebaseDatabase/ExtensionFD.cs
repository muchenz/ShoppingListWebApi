using Microsoft.Extensions.DependencyInjection;
using Shared.DataEndpoints.Abstaractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseDatabase
{
    public static class ExtensionFD
    {
        public static void AddFirebasedDatabase(this IServiceCollection services)
        {
            services.AddTransient<IUserEndpoint, UserEndpointFD>();
            services.AddTransient<IListAggregatorEndpoint, ListAggregatorEndpointFD>();
            services.AddTransient<IListItemEndpoint, ListItemEndpointFD>();
            services.AddTransient<IInvitationEndpoint, InvitationEndpointFD>();
            services.AddTransient<IListEndpoint, ListEndpointFD>();

            services.AddTransient<ITokenEndpoint, UserEndpointFD>();
            services.AddTransient<ToDeleteEndpoint>();


        }

    }
}
