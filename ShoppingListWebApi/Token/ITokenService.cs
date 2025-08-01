using Shared.DataEndpoints.Models.Requests;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingListWebApi.Token;
public interface ITokenService
{
    Task<(string accessToken, string refreshToken)> GenerateTokens(int userId);
    Task<(string newAccessToken, string newRefreshToken)> RefreshTokensAsync(int userId, string refreshToken);
    bool VerifyToken(string accessToken);
    Task<bool> VerifyAllTokens(int userId, string jti, VerifyAllTokensRequest tokens);
    Task<(string newAccessToken, string newRefreshToken)> RefreshTokensAsync2(int userId, string refreshToken, CancellationToken cancellationToken);
    

}