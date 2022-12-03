namespace MountAnything;

public interface IGetItemPropertiesParameters<in T>
{
    T GetPropertyParameters { set; }
}