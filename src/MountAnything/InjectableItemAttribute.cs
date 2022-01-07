namespace MountAnything;

/// <summary>
/// When placed on a <see cref="PathHandler"/> class, the item returned by the GetItem method
/// can be injected into PathHandlers that are children of this PathHandler in the routing hierarchy.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class InjectableItemAttribute : Attribute
{
    public Type ItemType { get; }

    public InjectableItemAttribute(Type itemType)
    {
        ItemType = itemType;
    }
}