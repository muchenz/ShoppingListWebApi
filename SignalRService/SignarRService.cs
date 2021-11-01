using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        public async Task SendRefreshMessageToUsersAsync(IEnumerable<int> usersIds, string command = null, int? id1 = null
            , int? listAggregationId = null, int? parentId = null, string signalRId = null)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

                try
                {
                    if (_hubConnection.State == HubConnectionState.Disconnected)
                    {
                        System.Diagnostics.Debug.Write("-------------------------------------------------HUB START ___________________________________");
                        await _hubConnection.StartAsync();
                    }

                    //  var aaa = await _context.UserListAggregators.Where(a => a.ListAggregatorId == listAggrId).Select(a => a.UserId).ToListAsync();
                    if (command == "Edit/Save_ListItem" || command == "Add_ListItem" || command == "Delete_ListItem")
                    {
                        await _hubConnection.SendAsync("SendAsyncListItem", usersIds, command, id1, listAggregationId
                            , parentId, signalRId);
                    }
                    else if (command == "New_Invitation")
                    {
                        await _hubConnection.SendAsync("SendAsyncNewIvitation", usersIds, signalRId);
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


    }

}
