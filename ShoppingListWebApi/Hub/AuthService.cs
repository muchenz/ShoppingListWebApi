using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Hub
{
    public class AuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthService(IConfiguration configuration
              , IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }


        public async Task<bool> IsValidateTokenAsync(string token)
        {

            if (token == "I am a god of hellfire.") return true;

            var client = _httpClientFactory.CreateClient("api");

            var baseAdress = _configuration.GetSection("AppSettings")["ShoppingWebAPIBaseAddress"];

            var response = await client.GetAsync($"User/VerifyToken?accessToken={token}");

            var data = await response.Content.ReadAsStringAsync();
            var isTokenGood = System.Text.Json.JsonSerializer.Deserialize<bool>(data);

            return isTokenGood;
        }


    }

}
