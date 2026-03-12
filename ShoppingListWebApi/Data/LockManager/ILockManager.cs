namespace ShoppingListWebApi.Data.LockManager;

public interface ILockManager
{
    LockManager.LockBuilder GetBuilder();
}