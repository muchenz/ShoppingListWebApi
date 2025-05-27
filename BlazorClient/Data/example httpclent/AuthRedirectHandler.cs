using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorClient.Data.example_httpclent;

public class AuthRedirectHandler:DelegatingHandler
{
    private readonly ITokenService _tokenService;

    public AuthRedirectHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 1. Dodaj access token do nagłówka
        var accessToken = await _tokenService.GetAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // 2. Wykonaj żądanie
        var response = await base.SendAsync(request, cancellationToken);

        // 3. Jeśli 401 - spróbuj odświeżyć token
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var refreshed = await _tokenService.TryRefreshTokenAsync();

            if (refreshed)
            {
                // 3a. Spróbuj ponownie z nowym tokenem
                var newAccessToken = await _tokenService.GetAccessTokenAsync();

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);

                // Klonuj oryginalne żądanie (ważne!)
                var clonedRequest = await CloneHttpRequestMessageAsync(request);

                return await base.SendAsync(clonedRequest, cancellationToken);
            }
        }

        return response;
    }

    private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = request.Content,
            Version = request.Version
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}

//------------------

//🔒 Bezpieczeństwo
//Trzymaj refresh token w SecureStorage(Xamarin/MAUI) lub protected storage w przeglądarce.

//Refresh token nie powinien być wysyłany nigdzie poza refresh endpointem.



public interface ITokenService
{
    Task<string> GetAccessTokenAsync();
    Task<bool> TryRefreshTokenAsync();
}

public interface ITokenApi
{
    Task<Res> GetAccessTokenAsync();
    Task<Res> RefreshTokenAsync(string a);
}

public class Res
{
    public bool IsSuccess { get; set; }
    public string NewAccessToken {  get; set; }
    public string NewRefreshToken {  get; set; }
}

public  class SecureStorage
{
    public static Task<string> GetAsync(string a) => throw new NotImplementedException();
    public static Task SetAsync(string a, string b) => throw new NotImplementedException();
}

public class TokenService : ITokenService
{
    private readonly ITokenApi _tokenApi;

    public TokenService(ITokenApi tokenApi)
    {
        _tokenApi = tokenApi;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        return await SecureStorage.GetAsync("access_token");
    }

    public async Task<bool> TryRefreshTokenAsync()
    {
        var refreshToken = await SecureStorage.GetAsync("refresh_token");

        if (string.IsNullOrEmpty(refreshToken))
            return false;

        var result = await _tokenApi.RefreshTokenAsync(refreshToken);
        if (result.IsSuccess)
        {
            await SecureStorage.SetAsync("access_token", result.NewAccessToken);
            await SecureStorage.SetAsync("refresh_token", result.NewRefreshToken);
            return true;
        }

        return false;
    }
}
