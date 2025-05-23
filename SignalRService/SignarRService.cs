using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.DataEndpoints.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SignalRService
{
    public class SignarRService
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;
        HubConnection _hubConnection;

        public SignarRService(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _hubConnection = new HubConnectionBuilder().WithUrl(_configuration.GetSection("SignalR")["ChatHub"], (opts) =>
            //_hubConnection = new HubConnectionBuilder().WithUrl(" https://127.0.0.1:5013/chatHub", (opts) =>
           {
               opts.Headers.Add("Access_Token", "I am a god of hellfire.");

               opts.HttpMessageHandlerFactory = (message) =>
               {
                   if (message is HttpClientHandler clientHandler)
                       // bypass SSL certificate
                       clientHandler.ServerCertificateCustomValidationCallback +=
                          (sender, certificate, chain, sslPolicyErrors) => { return true; };
                   return message;
               };
           }).WithAutomaticReconnect().Build();


        }

        public async Task SendRefreshMessageToUsersAsync(IEnumerable<int> usersIds, string eventName=null, string signalRId = null)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

                try
                {
                    await RestartHub();
                  
                    if (eventName == SiganalREventName.InvitationAreChanged)
                    {
                        await _hubConnection.SendAsync("SendAsyncIvitationChanged", usersIds, signalRId);
                    }
                    else
                    {
                        await _hubConnection.SendAsync("SendAsyncAllTree", usersIds, signalRId);
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "exxxxxxxxxxxxxx");
                }
            }
        }
        

        public async Task SendListItemRefreshMessageToUsersAsync(IEnumerable<int> usersIds, string eventName, string signalREvent)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

                try
                {
                    await RestartHub();

                    await _hubConnection.SendAsync("SendAsyncListItem", usersIds, eventName, signalREvent);


                }
                catch (Exception ex)
                {

                    logger.LogError(ex, "exxxxxxxxxxxxxx");
                }
            }

        }


        async Task RestartHub()
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                System.Diagnostics.Debug.Write("-------------------------------------------------HUB START ___________________________________");
                await _hubConnection.StartAsync();
            }
        }

    }

}
