namespace ShoppingListWebApi.Data.LockManager;

public interface ILockManager
{
    ILockBuilder GetBuilder();
}