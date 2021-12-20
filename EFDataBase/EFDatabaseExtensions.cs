using Microsoft.Extensions.DependencyInjection;
using Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFDataBase
{
    public static class EFDatabaseExtensions
    {
        public static void AddEFDatabase(this IServiceCollection services)
{
            services.AddTransient<IUserEndpoint, UserEndpoint>();
            services.AddTransient<IListAggregatorEndpoint, ListAggregatorEndpoint>();
            services.AddTransient<IListItemEndpoint, ListItemEndpoint>();
            services.AddTransient<IInvitationEndpoint, InvitationEndpoint>();
            services.AddTransient<IListEndpoint, ListEndpoint>();

        }
    }
}
