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

namespace BlazorClient.Data
{
    public class SignalRHelper
    {
        public static async Task SignalRInitAsync(IConfiguration  configuration,
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

            //_hubConnection = new HubConnectionBuilder().WithUrl("https://94.251.148.92:5013/chatHub", (opts) =>
            //{
            //_hubConnection = new HubConnectionBuilder().WithUrl("https://192.168.8.222:91/chatHub", (opts) =>
            //{
            HubConnection _hubConnection = new HubConnectionBuilder().WithUrl(configuration.GetSection("AppSettings")["SignlRAddress"], (opts) =>
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


            _hubConnection.On("DataAreChanged_" + data.UserId, async () =>
            {

                try
                {

                    var identity = await authenticationStateProvider.GetAuthenticationStateAsync();

                    var nameUser = identity.User.Identity.Name;

                    data = await userService.GetUserDataTreeObjectsgAsync(nameUser);

                    SetData(data);

                }

                catch(Exception ex)
                {

                    ((ShoppingListWebApi.Data.CustomAuthenticationStateProvider)authenticationStateProvider).MarkUserAsLoggedOut();

                    navigationManager.NavigateTo("/login");

                    return;
                }

                (var listAggregatorChoosed, var listChoosed) = await LoadSaveOrderHelper.LoadChoosedList(data, localStorage);

                SetListAggregatorChoosed(listAggregatorChoosed);
                SetListChoosed(listChoosed);

                await LoadSaveOrderHelper.LoadListAggregatorsOrder(localStorage, data, authenticationStateProvider);

                StateHasChanged();

                return;


            });

            _hubConnection.On("ListItemAreChanged_" + data.UserId, async (string command, int? id1, int? listAggregationId, int? parentId) =>
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
            });

            await _hubConnection.StartAsync();


        }
    }
}
