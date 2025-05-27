using Microsoft.AspNetCore.Components.Authorization;
using System.Threading.Tasks;

namespace BlazorClient.Data.example_httpclent;
public class AuthStateWatcher
{
    private readonly AuthenticationStateProvider _authProvider;
    private readonly SignalRService2 _signalR;

    private bool _initialized = false;

    public AuthStateWatcher(AuthenticationStateProvider authProvider, SignalRService2 signalR)
    {
        _authProvider = authProvider;
        _signalR = signalR;

        _authProvider.AuthenticationStateChanged += OnAuthStateChanged;
    }

    private async void OnAuthStateChanged(Task<AuthenticationState> task)
    {
        var state = await task;
        var user = state.User;

        if (user.Identity?.IsAuthenticated == true && !_initialized)
        {
            _initialized = true;

            await _signalR.StartConnectionAsync();
            //await _signalR.SubscribeToUserChannels(user.Identity.Name);
        }
        else if (!user.Identity?.IsAuthenticated ?? false)
        {
            _initialized = false;
            await _signalR.StopConnectionAsync();
        }
    }
}
