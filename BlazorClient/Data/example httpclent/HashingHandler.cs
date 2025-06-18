using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorClient.Data.example_httpclent;

public class HashingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var query = request.RequestUri?.Query;
        if (!string.IsNullOrWhiteSpace(query))
        {
            var queryParams = System.Web.HttpUtility.ParseQueryString(query);
            var userId = queryParams["userId"];

            if (!string.IsNullOrWhiteSpace(userId))
            {
                using var sha256 = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(userId);
                var hash = sha256.ComputeHash(bytes);
                var hashBase64 = Convert.ToBase64String(hash);

                request.Headers.Add("X-UserId-Hash", hashBase64);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}