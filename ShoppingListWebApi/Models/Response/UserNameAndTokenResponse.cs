namespace ShoppingListWebApi.Models.Response;

public class UserNameAndTokenResponse
{
    public string UserName { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
}
