using EFDataBase;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShoppingListWebApi.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Data
{
    public class WebApiHelper
    {


        public static async Task<IEnumerable<int>> GetuUserIdFromListAggrIdAsync(int listAggrId, ShopingListDBContext _context)
        {
            var userList = await _context.UserListAggregators.Where(a => a.ListAggregatorId == listAggrId).Select(a => a.UserId).ToListAsync();

            return userList;
        }




        public static async Task<MeResponse> GetFacebookUserFromCodeAsync(string code, string state, IConfiguration configuration)
        {

            string apiPrivateKey = configuration.GetSection("Secrets")["FacbookApiPrivateKey"];
            string appId = configuration.GetSection("Secrets")["FacbookAppId"];

            var querry = new QueryBuilder();
            querry.Add("client_id", appId);
            querry.Add("client_secret", apiPrivateKey);
            querry.Add("redirect_uri", "https://localhost:5001/api/User");
            querry.Add("code", code);


            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/oauth/access_token" + querry.ToString());
            requestMessage.Content = new StringContent("");

            requestMessage.Content.Headers.ContentType
                = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");


            //return RedirectPermanent("https://graph.facebook.com/oauth/access_token?client_id=259675572518658&client_secret=da7b7d35c4755ff27bb2051d803d7eef"
            //    + $"&redirect_uri=https://localhost:5001/api/User&code={code}");

            using var clientHttp = new HttpClient();
            clientHttp.BaseAddress = new Uri("https://graph.facebook.com");

            var restponse = await clientHttp.SendAsync(requestMessage);

            var tokenResponse = await System.Text.Json.JsonSerializer.DeserializeAsync<TokenResponse>(await restponse.Content.ReadAsStreamAsync());

            querry = new QueryBuilder();
            querry.Add("fields", "id,name,email");
            querry.Add("access_token", tokenResponse.access_token);

            requestMessage = new HttpRequestMessage(HttpMethod.Get, "/me" + querry.ToString());

            restponse = await clientHttp.SendAsync(requestMessage);

            var meResponse = await System.Text.Json.JsonSerializer.DeserializeAsync<MeResponse>(await restponse.Content.ReadAsStreamAsync());

            return meResponse;
        }

        public static async Task<MeResponse> GetFacebookUserFromTokenAsync(string token, string state, IConfiguration configuration)
        {

            var querry = new QueryBuilder();
            querry.Add("fields", "id,name,email");
            querry.Add("access_token", token);

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "/me" + querry.ToString());
            requestMessage.Content = new StringContent("");

            requestMessage.Content.Headers.ContentType
                = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");

            using var clientHttp = new HttpClient();
            clientHttp.BaseAddress = new Uri("https://graph.facebook.com");

            var restponse = await clientHttp.SendAsync(requestMessage);

            var meResponse = await System.Text.Json.JsonSerializer.DeserializeAsync<MeResponse>(await restponse.Content.ReadAsStreamAsync());

            return meResponse;
        }
    }
}
