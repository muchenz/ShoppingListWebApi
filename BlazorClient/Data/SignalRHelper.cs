using BlazorClient.Models;
using BlazorClient.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorClient.Data
{
    public class SignalRHelper
    {
        /*

        static HubConnection _hubConnection;

        static int amount = 0;
        public static async Task SignalRInitAsync(IConfiguration configuration)
        {
            Console.WriteLine("singnalR amount" + amount++);
            Console.WriteLine("singnalR ststus" + _hubConnection?.State);
            //_hubConnection = new HubConnectionBuilder().WithUrl("https://94.251.148.187:5013/chatHub", (opts) =>
            //{
            //_hubConnection = new HubConnectionBuilder().WithUrl("https://192.168.8.222:91/chatHub", (opts) =>
            //{
            _hubConnection = new HubConnectionBuilder().WithUrl(configuration.GetSection("AppSettings")["SignlRAddress"], (opts) =>
            {
                opts.HttpMessageHandlerFactory = (message) =>
                {
                    if (message is HttpClientHandler clientHandler)
                        // bypass SSL certificate
                        clientHandler.ServerCertificateCustomValidationCallback +=
                                  (sender, certificate, chain, sslPolicyErrors) => { return true; };
                    return message;
                };
            }).WithAutomaticReconnect().Build();

            await _hubConnection.StartAsync();

        }



        */
        public static async Task<List<IDisposable>> SignalRInvitationInitAsync(
            HubConnection _hubConnection,
            int userId,
            AuthenticationStateProvider authenticationStateProvider,
            UserService userService,
             Action<List<Invitation>> SetInvitation,
             Action<int> SetInvitationCount,
             Func<Task> StateHasChangedAsync
            )
        {

            IDisposable dataAreChanged = null;

            try
            {
                var authProvider = await authenticationStateProvider.GetAuthenticationStateAsync();

                dataAreChanged = _hubConnection.On("InvitationAreChaned_" + userId, async () =>
                {


                    var userName = authProvider.User.Identity.Name;

                    var invitationsList = await userService.GetInvitationsListAsync(userName);

                    int count = invitationsList?.Count ?? 0; // == null ? 0 : invitationsList.Count;

                    SetInvitation(invitationsList);
                    SetInvitationCount(count);
                    await StateHasChangedAsync();

                });

            }
            catch
            {

            }

            return new List<IDisposable> { dataAreChanged };
        }

        public static List<IDisposable> SignalRShoppingListInit(
             HubConnection _hubConnection,
            User data,
            Action<User> SetData,
            Action<ListAggregator> SetListAggregatorChoosed,
            Action<List> SetListChoosed,

            AuthenticationStateProvider authenticationStateProvider, UserService userService,
            NavigationManager navigationManager, Func<Task> StateHasChangedAysnc,
            ShoppingListService shoppingListService,
            ILocalStorageService localStorage

            )
        {

            List<IDisposable> disposables = new List<IDisposable>();

            ////_hubConnection = new HubConnectionBuilder().WithUrl("https://94.251.148.187:5013/chatHub", (opts) =>
            ////{
            ////_hubConnection = new HubConnectionBuilder().WithUrl("https://192.168.8.222:91/chatHub", (opts) =>
            ////{
            //HubConnection _hubConnection = new HubConnectionBuilder().WithUrl(configuration.GetSection("AppSettings")["SignlRAddress"], (opts) =>
            //{
            //    opts.HttpMessageHandlerFactory = (message) =>
            //    {
            //        if (message is HttpClientHandler clientHandler)
            //            // bypass SSL certificate
            //            clientHandler.ServerCertificateCustomValidationCallback +=
            //                       (sender, certificate, chain, sslPolicyErrors) => { return true; };
            //        return message;
            //    };
            //}).WithAutomaticReconnect().Build();


            var dataAreChanged = _hubConnection.On("DataAreChanged_" + data.UserId, async () =>
           {

               try
               {

                   data = await userService.GetUserDataTreeAsync();

                   SetData(data);

               }

               catch (Exception ex)
               {

                   ((CustomAuthenticationStateProvider)authenticationStateProvider).MarkUserAsLoggedOut();

                   navigationManager.NavigateTo("/login");

                   return;
               }

               (var listAggregatorChoosed, var listChoosed) = await LoadSaveOrderHelper.LoadChoosedList(data, localStorage);

               SetListAggregatorChoosed(listAggregatorChoosed);
               SetListChoosed(listChoosed);

               await LoadSaveOrderHelper.LoadListAggregatorsOrder(localStorage, data, authenticationStateProvider);

               await StateHasChangedAysnc();

               return;


           });

            var listAreChaned = _hubConnection.On("ListItemAreChanged_" + data.UserId, async (string signaREnvelope) =>
            {
                var envelope = JsonSerializer.Deserialize<SignaREnvelope>();
                var evenName = envelope.SiglREventName;
                var signaREventSerialized = envelope.SerializedEvent



                switch (eventName)
                {
                    case SiganalREventName.ListItemEdited:
                        {
                            var signaREvent = JsonSerializer.Deserialize<ListItemEditedSignalREvent>(signaREventSerialized);
                            var item = await shoppingListService.GetItem<ListItem>(signaREvent.ListItemId, signaREvent.ListAggregationId);

                            var lists = data.ListAggregators.Where(a => a.ListAggregatorId == signaREvent.ListAggregationId).FirstOrDefault();

                            ListItem foundListItem = null;
                            foreach (var listItem in lists.Lists)
                            {
                                foundListItem = listItem.ListItems.FirstOrDefault(a => a.Id == signaREvent.ListItemId);
                                if (foundListItem != null) break;
                            }
                            if (foundListItem == null) return;
                            foundListItem.ListItemName = item.ListItemName;
                            foundListItem.State = item.State;
                            await StateHasChangedAysnc();
                            break;
                        }
                    case SiganalREventName.ListItemAdded:
                        {
                            var signaREvent = JsonSerializer.Deserialize<ListItemAddedSignalREvent>(signaREventSerialized);
                            var item = await shoppingListService.GetItem<ListItem>(signaREvent.ListItemId, signaREvent.ListAggregationId);

                            var tempList = data.ListAggregators.Where(a => a.ListAggregatorId == signaREvent.ListAggregationId).FirstOrDefault().
                                Lists.Where(a => a.ListId == signaREvent.ListId).FirstOrDefault();
                            if (!tempList.ListItems.Any(a => a.ListItemId == item.ListItemId))
                            {
                                tempList.ListItems.Insert(0, item);
                            }
                            //tempList.ListItems=new List<ListItem>() { };

                            await StateHasChangedAysnc();
                            break;
                        }
                    case SiganalREventName.ListItemDeleted:
                        {
                            var signaREvent = JsonSerializer.Deserialize<ListItemDeletedSignalREvent>(signaREventSerialized);

                            var lists = data.ListAggregators.Where(a => a.ListAggregatorId == signaREvent.ListAggregationId).FirstOrDefault();

                            ListItem foundListItem = null;
                            List founfList = null;

                            foreach (var listItem in lists.Lists)
                            {
                                founfList = listItem;
                                foundListItem = listItem.ListItems.FirstOrDefault(a => a.Id == signaREvent.ListItemId);
                                if (foundListItem != null) break;
                            }
                            if (foundListItem == null) return;

                            founfList.ListItems.Remove(foundListItem);

                            await StateHasChangedAysnc();
                            break;
                        }
                    default:
                        break;
                }
            });



            disposables.Add(dataAreChanged);
            disposables.Add(listAreChaned);

            return disposables;
        }
    }
}
