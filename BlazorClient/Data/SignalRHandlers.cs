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
    public class SignalRHandlers
    {

        public static async Task SignalRInvitationInitAsync(
            UserService userService,
             Action<List<Invitation>> SetInvitation,
             Action<int> SetInvitationCount,
             Func<Task> StateHasChangedAsync
            )
        {
            try
            {
                var invitationsList = await userService.GetInvitationsListAsync();

                int count = invitationsList?.Count ?? 0; // == null ? 0 : invitationsList.Count;

                SetInvitation(invitationsList);
                SetInvitationCount(count);
                await StateHasChangedAsync();

            }
            catch
            {

            }

        }

        public static async Task SignalRGetUserDataTreeAsync(
            User data,
            Action<User> SetData,
            Action<ListAggregator> SetListAggregatorChoosed,
            Action<List> SetListChoosed,
            AuthenticationStateProvider authenticationStateProvider,
            UserService userService,
            NavigationManager navigationManager, 
            Func<Task> StateHasChangedAysnc,
            ShoppingListService shoppingListService,
            ILocalStorageService localStorage

            )
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

        }

        public static async Task SignalRListItemAreChangedAsync(
       SignaREnvelope envelope,
       User data,
       Action<User> SetData,
       Action<ListAggregator> SetListAggregatorChoosed,
       Action<List> SetListChoosed,
       AuthenticationStateProvider authenticationStateProvider, 
       UserService userService,
       NavigationManager navigationManager, 
       Func<Task> StateHasChangedAysnc,
       ShoppingListService shoppingListService,
       ILocalStorageService localStorage

       )
        {
            //var envelope = JsonSerializer.Deserialize<SignaREnvelope>(signaREnvelope);
            var eventName = envelope.SiglREventName;
            var signaREventSerialized = envelope.SerializedEvent;



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
        }

    }
}

