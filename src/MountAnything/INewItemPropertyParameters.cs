namespace MountAnything;

public interface INewItemPropertyParameters<in T> where T : new()
{
    T NewItemPropertyParameters { set; }
}