//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.ActionConstraints;
//using System.Threading.Tasks;

//namespace BlazorClient.Data.example_httpclent;

//public class TokenService
//{
//    private readonly ITokenApi _tokenApi;

//    public TokenService(ITokenApi tokenApi)
//    {
//        _tokenApi = tokenApi;
//    }

//    public async Task<string> GetAccessTokenAsync()
//    {
//        return await SecureStorage.GetAsync("access_token");
//    }

//    public async Task<bool> TryRefreshTokenAsync()
//    {
//        var refreshToken = await SecureStorage.GetAsync("refresh_token");

//        if (string.IsNullOrEmpty(refreshToken))
//            return false;

//        var result = await _tokenApi.RefreshTokenAsync(refreshToken);
//        if (result.IsSuccess)
//        {
//            await SecureStorage.SetAsync("access_token", result.NewAccessToken);
//            await SecureStorage.SetAsync("refresh_token", result.NewRefreshToken);
//            return true;
//        }

//        return false;
//    }
//}

//public interface ITokenService
//{
//    Task<string> GetAccessTokenAsync();
//    Task<bool> TryRefreshTokenAsync();
//}

////Trzymaj refresh token w SecureStorage (Xamarin/MAUI) lub protected storage w przeglądarce.