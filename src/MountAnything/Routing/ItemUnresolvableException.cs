namespace MountAnything.Routing;

public class ItemUnresolvableException : ApplicationException
{
    public ItemUnresolvableException(string message) : base(message) {}
}