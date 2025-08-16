using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.DataEndpoints.Abstaractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

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

            services.AddSingleton<DeleteChannel>();

            using var serviceProvider = services.BuildServiceProvider();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var section = configuration.GetSection("FirebaseFD");

            var options = new FirebaseFDOptions();
            section.Bind(options);
            services.AddSingleton(_ => options);

        }

    }
}
internal class FirebaseFDOptions
{
    public bool UseBatchProcessing { get; set; }
}

internal sealed class DeleteChannel
{
    private readonly Channel<DeleteEvent> _messages = Channel.CreateUnbounded<DeleteEvent>();

    public ChannelReader<DeleteEvent> Reader => _messages.Reader;
    public ChannelWriter<DeleteEvent> Writer => _messages.Writer;
}

internal record DeleteEvent();