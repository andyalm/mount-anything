namespace MountAnything;

public interface ISetItemParameters<in T>
{
    T SetItemParameters { set; }
}