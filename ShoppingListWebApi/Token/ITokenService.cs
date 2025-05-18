using System.Threading.Tasks;

namespace ShoppingListWebApi.Token;
public interface ITokenService
{
    Task<string> GenerateToken(int userId);
    bool VerifyToken(string accessToken);
}