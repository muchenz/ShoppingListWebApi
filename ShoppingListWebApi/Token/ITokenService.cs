using System.Threading.Tasks;

namespace ShoppingListWebApi.Token;
public interface ITokenService
{
    Task<(string accessToken, string refreshToken)> GenerateTokens(int userId);
    Task<(string newAccessToken, string newRefreshToken)> RefreshTokensAsync(int userId, string refreshToken);
    bool VerifyToken(string accessToken);
}