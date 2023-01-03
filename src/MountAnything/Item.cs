using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Management.Automation;
using System.Reflection;

namespace MountAnything;

public abstract class Item<T> : IItem where T : class
{
    private static ConcurrentDictionary<Type, PropertyInfo[]> _itemPropertyCache = new();

    protected Item(ItemPath parentPath, T underlyingObject)
    {
        ParentPath = parentPath;
        UnderlyingObject = underlyingObject;
    }
    
    public ItemPath ParentPath { get; }
    public ItemPath FullPath => ParentPath.Combine(ItemName);
    public abstract string ItemName { get; }
    public abstract bool IsContainer { get; }
    public virtual bool IsPartial { get; }
    public T UnderlyingObject { get; }

    public virtual string? ItemType => null;
    protected virtual string TypeName => GetType().FullName!;
    public virtual IEnumerable<string> Aliases => Enumerable.Empty<string>();

    public IEnumerable<ItemPath> CacheablePaths
    {
        get
        {
            yield return FullPath;
            foreach (var alias in Aliases)
            {
                yield return ParentPath.Combine(alias);
            }
        }
    }

    protected virtual void CustomizePSObject(PSObject psObject) {}

    public PSObject ToPipelineObject(Func<ItemPath,string> pathResolver)
    {
        var psObject = UnderlyingObject is PSObject underlyingObject ? underlyingObject : new PSObject(UnderlyingObject);
        psObject.SetTypeName(TypeName);
        SetPropertiesFromPropertyItemAttributes(psObject);
        psObject.SetPropertyIfMissing(nameof(ItemName), ItemName);
        psObject.SetPropertyIfMissing("Name", ItemName);
        if (ItemType != null)
        {
            psObject.SetProperty(nameof(ItemType), ItemType);
        }
        SetLinks(pathResolver, psObject);
        CustomizePSObject(psObject);

        return psObject;
    }

    private void SetPropertiesFromPropertyItemAttributes(PSObject psObject)
    {
        var properties = _itemPropertyCache.GetOrAdd(GetType(), t => t.GetProperties().Where(p => p.GetCustomAttribute<ItemPropertyAttribute>() != null).ToArray());

        foreach (var itemProperty in properties)
        {
            var propertyName = itemProperty.GetCustomAttribute<ItemPropertyAttribute>()?.PropertyName ??
                               itemProperty.Name;

            var propertyValue = itemProperty.GetValue(this);
            psObject.SetProperty(propertyName, propertyValue);
        }
    }

    private void SetLinks(Func<ItemPath, string> pathResolver, PSObject psObject)
    {
        foreach (var link in Links)
        {
            psObject.SetProperty(link.Key, link.Value.ToPipelineObject(pathResolver));
        }

        var linkObject = new PSObject();
        foreach (var link in Links)
        {
            linkObject.Properties.Add(new PSNoteProperty(link.Key, pathResolver(link.Value.FullPath)));
        }

        foreach (var linkPath in LinkPaths)
        {
            linkObject.Properties.Add(new PSNoteProperty(linkPath.Key, pathResolver(linkPath.Value)));
        }

        psObject.Properties.Add(new PSNoteProperty(nameof(Links), linkObject));
    }

    public IDictionary<string,IItem> Links { get; protected init; } = ImmutableDictionary<string, IItem>.Empty;
    public IDictionary<string,ItemPath> LinkPaths { get; protected init; } = ImmutableDictionary<string, ItemPath>.Empty;
}

public abstract class Item : Item<PSObject>
{
    protected Item(ItemPath parentPath, PSObject underlyingObject) : base(parentPath, underlyingObject)
    {
    }
    
    protected Item(ItemPath parentPath, object underlyingObject) : base(parentPath, new PSObject(underlyingObject))
    {
    }

    protected T? Property<T>(string name)
    {
        return UnderlyingObject.Property<T?>(name);
    }
}