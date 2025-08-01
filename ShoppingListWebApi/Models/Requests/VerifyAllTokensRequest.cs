namespace Shared.DataEndpoints.Models.Requests;

public class VerifyAllTokensRequest
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}
