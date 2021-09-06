using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BlazorClient.Services;
using Blazored.LocalStorage;

namespace BlazorClient.Handlers
{
    public class CustomAuthorizationHeaderHandler : DelegatingHandler
    {

        public CustomAuthorizationHeaderHandler()
        {
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //var aaaaaaaaaaa = UserInfoService.TokenList;
            //if (UserInfoService.Token != null)
            //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", UserInfoService.Token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}