namespace MountAnything;

/// <summary>
/// When placed on a public property of an item inheriting from <see cref="Item"/>,
/// then that property will be added to the item when its written to the powershell pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ItemPropertyAttribute : Attribute
{
    /// <summary>
    /// The name of the property when written to the powershell pipeline. If <c>null</c>,
    /// then the name of the property that the attribute is associated with will be used.
    /// </summary>
    public string? PropertyName { get; }

    public ItemPropertyAttribute() { }

    public ItemPropertyAttribute(string propertyName)
    {
        PropertyName = propertyName;
    }
}