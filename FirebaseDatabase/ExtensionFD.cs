using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.DataEndpoints.Abstaractions;
using ShoppingListWebApi.Data;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;


namespace FirebaseDatabase
{
    public static class ExtensionFD
    {
        public static void AddFirebasedDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IUserEndpoint, UserEndpointFD>();
            services.AddTransient<IListAggregatorEndpoint, ListAggregatorEndpointFD>();
            services.AddTransient<IListItemEndpoint, ListItemEndpointFD>();
            services.AddTransient<IInvitationEndpoint, InvitationEndpointFD>();
            services.AddTransient<IListEndpoint, ListEndpointFD>();
            services.AddTransient<ITokenEndpoint, UserEndpointFD>();


            services.AddTransient<ToDeleteEndpoint>();
            services.AddSingleton<DeleteChannel>();

            services.Configure<FirebaseFDOptions>(configuration.GetSection(FirebaseFDOptions.SectionName));


            var firebaseOptions = configuration.GetSection(FirebaseFDOptions.SectionName).Get<FirebaseFDOptions>();

            if (firebaseOptions.UseBatchProcessing)
            {
                services.AddHostedService<ToDeleteService>();
            }
        }

    }
}
internal class FirebaseFDOptions
{
    public const string SectionName = "FirebaseFD";
    public bool UseBatchProcessing { get; set; } = true;
    public bool UseChannel { get; set; } = true;
    public int PollingDelay { get; set; } = 1000;

}

internal sealed class DeleteChannel
{
    private readonly Channel<DeleteEvent> _messages = Channel.CreateUnbounded<DeleteEvent>();

    public ChannelReader<DeleteEvent> Reader => _messages.Reader;
    public ChannelWriter<DeleteEvent> Writer => _messages.Writer;
}

public record DeleteEvent();