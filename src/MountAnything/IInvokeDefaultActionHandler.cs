namespace MountAnything;

public interface IInvokeDefaultActionHandler
{
    IEnumerable<IItem>? InvokeDefaultAction();
}