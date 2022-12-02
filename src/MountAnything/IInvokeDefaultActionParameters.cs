namespace MountAnything;

public interface IInvokeDefaultActionParameters<in T> where T : new()
{
    T InvokeDefaultActionParameters { set; }
}