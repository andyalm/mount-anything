namespace MountAnything;

public interface IRemoveItemPropertyParameters<in T> where T : new()
{
    T RemoveItemPropertyParameters { set; }
}