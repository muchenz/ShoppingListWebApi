using BlazorClient.Data;
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
using System.Threading.Tasks;

namespace BlazorClient.Data_COPY
{
    public class SignalRHelper
    {

        static event Action DataChangedEvent;
        static event Action<string,int?,int?,int?> ListItemChangedEvent;


        static Action<Action> AddDataChangedDelegate = a => DataChangedEvent += a;
        static Action<Action> RemoveDataChangedDelegate = a => DataChangedEvent -= a;

        static Action<Action<string, int?, int?, int?>> AddListItemChangedDelegate = a => ListItemChangedEvent += a;
        static Action<Action<string, int?, int?, int?>> RemoveListItemChangedDelegate = a => ListItemChangedEvent -=a ;





        static HubConnection _hubConnection;

        public static async Task SignalRInitAsync(IConfiguration configuration)
        {
            //_hubConnection = new HubConnectionBuilder().WithUrl("https://94.251.148.92:5013/chatHub", (opts) =>
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
        }


        public static async Task DataAreChanged_For_Shoppilg_List(AuthenticationStateProvider authenticationStateProvider,
          UserService userService,
          User data,
           Action<User> SetData,
            NavigationManager navigationManager,
             Action<ListAggregator> SetListAggregatorChoosed,
             Action<List> SetListChoosed,
             ILocalStorageService localStorage,
             Action StateHasChanged
          )
        {
            try
            {

                var identity = await authenticationStateProvider.GetAuthenticationStateAsync();

                var nameUser = identity.User.Identity.Name;

                data = await userService.GetUserDataTreeObjectsgAsync(nameUser);

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

            StateHasChanged();

            return;

        }


        public static async Task ListItemAreChanged__For_Shoppig_List(

            string command, int? id1, int? listAggregationId, int? parentId,
             ShoppingListService shoppingListService,
             User data,
             Action StateHasChanged

            )
        {

            if (command.EndsWith("ListItem"))
            {
                var item = await shoppingListService.GetItem<ListItem>((int)id1, (int)listAggregationId);

                if (command == "Edit/Save_ListItem")
                {
                    var lists = data.ListAggregators.Where(a => a.ListAggregatorId == listAggregationId).FirstOrDefault();

                    ListItem foundListItem = null;
                    foreach (var listItem in lists.Lists)
                    {
                        foundListItem = listItem.ListItems.FirstOrDefault(a => a.Id == id1);
                        if (foundListItem != null) break;
                    }
                    if (foundListItem == null) return;
                    foundListItem.ListItemName = item.ListItemName;
                    foundListItem.State = item.State;
                    StateHasChanged();


                }
                else
                     if (command == "Add_ListItem")
                {


                    data.ListAggregators.Where(a => a.ListAggregatorId == listAggregationId).FirstOrDefault().
                   Lists.Where(a => a.ListId == parentId).FirstOrDefault().ListItems.Add(item);

                    StateHasChanged();
                }
                else
                         if (command == "Delete_ListItem")
                {

                    var lists = data.ListAggregators.Where(a => a.ListAggregatorId == listAggregationId).FirstOrDefault();

                    ListItem foundListItem = null;
                    List founfList = null;

                    foreach (var listItem in lists.Lists)
                    {
                        founfList = listItem;
                        foundListItem = listItem.ListItems.FirstOrDefault(a => a.Id == id1);
                        if (foundListItem != null) break;
                    }
                    if (foundListItem == null) return;

                    founfList.ListItems.Remove(foundListItem);

                    StateHasChanged();

                }
            }
        }
        public static async Task SignalRRunAsync(IConfiguration configuration,
            User data,
            Action<User> SetData,
            Action<ListAggregator> SetListAggregatorChoosed,
            Action<List> SetListChoosed,

            AuthenticationStateProvider authenticationStateProvider, UserService userService,
            NavigationManager navigationManager, Action StateHasChanged,
            ShoppingListService shoppingListService,
            ILocalStorageService localStorage

            )
        {

            await SignalRInitAsync(configuration);


            _hubConnection.On("DataAreChanged_" + data.UserId, async () =>
            {

                await DataAreChanged_For_Shoppilg_List(authenticationStateProvider,
                                                    userService,
                                                    data,
                                                    SetData,
                                                    navigationManager,
                                                    SetListAggregatorChoosed,
                                                    SetListChoosed,
                                                    localStorage,
                                                    StateHasChanged);

            });

            _hubConnection.On("ListItemAreChanged_" + data.UserId, async (string command, int? id1, int? listAggregationId, int? parentId) =>
            {

                await ListItemAreChanged__For_Shoppig_List(command, id1, listAggregationId, parentId,
                                                            shoppingListService,
                                                            data,
                                                            StateHasChanged);


            });


            await SignalRStartAsync();

        }

        public static async Task SignalRStartAsync()
        {
            await _hubConnection.StartAsync();
        }
    }
}
