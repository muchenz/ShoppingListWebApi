using Shared.DataEndpoints.Models.Requests;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Token;
public interface ITokenService
{
    Task<(string accessToken, string refreshToken)> GenerateTokens(int userId, string deviceId,  int? version = null);
    Task<(string newAccessToken, string newRefreshToken)> RefreshTokensAsync(int userId, string refreshToken);
    bool VerifyToken(string accessToken);
    Task<bool> VerifyAcceessRefreshTokens(int userId, string jti, VerifyAccessRefreshTokenRequest tokens);
    Task<(string newAccessToken, string newRefreshToken)> RefreshTokensAsync2(int userId, string deviceId, string refreshToken, int version,  CancellationToken cancellationToken);
    

}