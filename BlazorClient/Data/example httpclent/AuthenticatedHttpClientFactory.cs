using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShoppingList_WebClient.Data.example_httpclent;

public class AuthenticatedHttpClientFactory
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigation;

    public AuthenticatedHttpClientFactory(IHttpClientFactory httpFactory,
        ILocalStorageService localStorage,
        NavigationManager navigation)
    {
        _httpFactory = httpFactory;
        _localStorage = localStorage;
        _navigation = navigation;
    }

    public async Task<HttpClient> GetClientAsync()
    {
        var client = _httpFactory.CreateClient("Api");

        var token = await _localStorage.GetItemAsync<string>("access_token");
        var refresh = await _localStorage.GetItemAsync<string>("refresh_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            _navigation.NavigateTo("/login");
            return client;
        }

        if (IsJwtExpired(token))
        {
            token = await RefreshTokenAsync(refresh);
            if (string.IsNullOrWhiteSpace(token))
            {
                _navigation.NavigateTo("/login");
                return client;
            }

            await _localStorage.SetItemAsync("access_token", token);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static bool IsJwtExpired(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) return true;

        var payload = parts[1];
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(payload)));
        var obj = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        if (obj != null && obj.TryGetValue("exp", out var exp))
        {
            var expUnix = exp.GetInt64();
            var expTime = DateTimeOffset.FromUnixTimeSeconds(expUnix);
            return expTime < DateTimeOffset.UtcNow;
        }

        return true;
    }

    private static string PadBase64(string base64) => base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

    private async Task<string?> RefreshTokenAsync(string refreshToken)
    {
        var client = _httpFactory.CreateClient("Api");

        var response = await client.PostAsJsonAsync("auth/refresh", new { RefreshToken = refreshToken });
        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return result?.AccessToken;
    }
}
public class TokenResponse
{
    public string AccessToken { get; set; } = "";
}