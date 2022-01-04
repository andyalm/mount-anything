using System.Collections.Immutable;
using System.Management.Automation;

namespace MountAnything;

public abstract class Item<T> : IItem where T : class
{
    protected Item(ItemPath parentPath, T underlyingObject)
    {
        ParentPath = parentPath;
        UnderlyingObject = underlyingObject;
    }
    
    public ItemPath ParentPath { get; }
    public ItemPath FullPath => ParentPath.Combine(ItemName);
    public abstract string ItemName { get; }
    public abstract bool IsContainer { get; }
    public T UnderlyingObject { get; }

    public virtual string? ItemType => null;
    protected virtual string TypeName => UnderlyingObject.GetType().FullName!;
    
    protected virtual IEnumerable<string> Aliases => Enumerable.Empty<string>();

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

    protected override string TypeName => UnderlyingObject.TypeNames.FirstOrDefault() ?? typeof(PSObject).FullName!;

    protected T? Property<T>(string name)
    {
        return UnderlyingObject.Property<T?>(name);
    }
}