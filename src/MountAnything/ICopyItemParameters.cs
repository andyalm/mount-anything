namespace MountAnything;

public interface ICopyItemParameters<in T> where T : new()
{
    T CopyItemParameters { set; }
}