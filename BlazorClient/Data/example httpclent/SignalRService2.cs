using BlazorClient.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

public class SignalRService2
{
    private readonly NavigationManager _navigation;
    private readonly ILocalStorageService _localStorage;
    private HubConnection? _connection;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public SignalRService2(NavigationManager navigation, ILocalStorageService localStorage)
    {
        _navigation = navigation;
        _localStorage = localStorage;
    }

    public async Task StartConnectionAsync()
    {
        if (_connection != null && _connection.State == HubConnectionState.Connected)
            return;

        var token = await _localStorage.GetItemAsync<string>("access_token");
        if (string.IsNullOrWhiteSpace(token))
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl(_navigation.ToAbsoluteUri("/hub/notifications"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token)!;
            })
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();

        try
        {
            await _connection.StartAsync();
            Console.WriteLine("SignalR connected.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR connection failed: {ex.Message}");
        }
    }

    public async Task StopConnectionAsync()
    {
        if (_connection != null)
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    //----------- e.g------------------
    public event Action<ListItem>? OnNewListItemReceived;

    private void RegisterHandlers()
    {
        _connection?.On<ListItem>("ReceiveListItem", (item) =>
        {
            Console.WriteLine("Received ListItem from SignalR");
            OnNewListItemReceived?.Invoke(item);
        });
    }
    //-----------------------------------------------


    public async Task SendMessage(string message)
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            await _connection.InvokeAsync("SendMessage", message);
        }
    }

    public async Task SubscribeToUserChannels(string? username)
    {
        if (!string.IsNullOrEmpty(username))
        {
            await _connection!.InvokeAsync("JoinGroup", $"user:{username}");
        }
    }


}
