namespace ShoppingListWebApi.Models.Requests;

public class InviteUserRequest
{
    public int Permission { get; set; }
    public string UserName { get; set; }
}