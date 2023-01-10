namespace MountAnything;

public interface INewItemPropertyHandler
{
    void NewItemProperty(string propertyName, string? propertyTypeName, object propertyValue);
}