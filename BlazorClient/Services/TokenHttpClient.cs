using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorClient.Services;

public class TokenHttpClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenClientService _tokenClientService;
    private readonly StateService _stateService;

    public TokenHttpClient(IHttpClientFactory httpClientFactory, TokenClientService tokenClientService, StateService stateService)
    {
        _httpClientFactory = httpClientFactory;
        _tokenClientService = tokenClientService;
        _stateService = stateService;
    }


    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var httpClient = _httpClientFactory.CreateClient("api");

        await _tokenClientService.CheckAndSetNewTokens();

        var signalRId = _stateService.StateInfo.ClientSignalRID;

        request.Headers.Add("SignalRId", signalRId);

        var accessToken = _stateService.StateInfo.Token;


        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = null; try
        {
            response = await httpClient.SendAsync(request);
        }
        catch(Exception ex)
        {

        }
        //if(response.StatusCode== HttpStatusCode.Unauthorized && response.Headers.TryGetValues("Token-Expired", out var values))
        //{
        //    await _tokenClientService.RefreshTokensAsync();
        //    accessToken = _stateService.StateInfo.Token;
        //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        //    request = CloneRequest(request);
        //    response = await httpClient.SendAsync(request);
        //}
                
        return response;
    }

    private HttpRequestMessage CloneRequest(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = request.Content, // only if content may be used many times (not stream or files)
            Version = request.Version
        };

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var property in request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object>(property.Key), property.Value);

        return clone;
    }
}
