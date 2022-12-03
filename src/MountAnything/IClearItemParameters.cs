namespace MountAnything;

public interface IClearItemParameters<in T> where T : new()
{
    T ClearItemParameters { set; }
}