using System.Threading.Tasks;

namespace ShoppingListWebApi.Token;
public interface ITokenService
{
    Task<(string accessToken, string refreshToken)> GenerateTokens(int userId);
    bool VerifyToken(string accessToken);
}