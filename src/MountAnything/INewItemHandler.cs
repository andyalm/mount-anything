namespace MountAnything;

public interface INewItemHandler
{
    IItem NewItem(string? itemTypeName, object? newItemValue);
}