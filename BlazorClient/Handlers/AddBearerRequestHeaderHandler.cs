using Blazored.LocalStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorClient.Handlers
{
    public class AddBearerRequestHeaderHandler:DelegatingHandler
    {
        private readonly ILocalStorageService _localStorageService;

        public AddBearerRequestHeaderHandler(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorageService.GetItemAsync<string>("accessToken");

            if (token != null)
            {

                request.Headers.Authorization
                    = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            }

            return await base.SendAsync(request, cancellationToken);
           
        }
    }
}
