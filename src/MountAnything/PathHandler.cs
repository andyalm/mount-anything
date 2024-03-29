namespace MountAnything;

public abstract class PathHandler : IPathHandler
{
    protected PathHandler(ItemPath path, IPathHandlerContext context)
    {
        Path = path;
        Context = context;
        LinkGenerator = new LinkGenerator(path);
    }
    
    public ItemPath Path { get; }
    protected IPathHandlerContext Context { get; }
    
    protected LinkGenerator LinkGenerator { get; }

    protected Cache Cache => Context.Cache;
    protected void WriteDebug(string message) => Context.WriteDebug(message);
    protected void WriteWarning(string message) => Context.WriteWarning(message);

    protected ItemPath ParentPath => Path.Parent;
    protected string ItemName => Path.Name;

    public bool Exists()
    {
        if (Cache.TryGetItem(Path, out _))
        {
            return true;
        }

        return ExistsImpl();
    }

    public IItem? GetItem(Freshness? freshness = null)
    {
        freshness ??= Freshness.NoPartial;
        if (Cache.TryGetItem(Path, out var cachedItem) && freshness.IsFresh(cachedItem.FreshnessTimestamp, cachedItem.Item.IsPartial, Context.Force))
        {
            return cachedItem.Item;
        }

        var item = GetItemImpl();
        if (item != null)
        {
            WriteDebug($"Cache.SetItem<{item.GetType().Name}>({item.FullPath})");
            Cache.SetItem(item);
        }

        return item;
    }

    public IEnumerable<IItem> GetChildItems(Freshness? freshness = null)
    {
        freshness ??= Freshness.Default;
        if (CacheChildren && Cache.TryGetChildItems(Path, out var cachedObject)
                          && freshness.IsFresh(cachedObject.FreshnessTimestamp, false, Context.Force))
        {
            WriteDebug($"True Cache.TryGetChildItems({Path})");
            return cachedObject.ChildItems;
        }
        WriteDebug($"False Cache.TryGetChildItems({Path})");

        var item = GetItem(Freshness.Default);
        if (item != null)
        {
            var childItems = GetChildItemsImpl().ToArray();
            WriteDebug($"Cache.SetChildItems({item.FullPath}, {childItems.Length})");
            if (CacheChildren)
            {
                Cache.SetChildItems(item, childItems);
            }

            return childItems;
        }
        
        return Enumerable.Empty<IItem>();
    }

    protected virtual bool ExistsImpl() => GetItem(Freshness.Fastest) != null;
    protected abstract IItem? GetItemImpl();
    protected abstract IEnumerable<IItem> GetChildItemsImpl();

    protected virtual bool CacheChildren => true;
    public virtual Freshness GetItemCommandDefaultFreshness => Freshness.Guaranteed;
    public virtual Freshness GetChildItemsCommandDefaultFreshness => Freshness.Guaranteed;

    public virtual IEnumerable<IItem> GetChildItems(string filter)
    {
        return GetChildItems(Freshness.Default)
            .Where(i => i.MatchesPattern(Path.Combine(filter)));
    }

    public virtual IEnumerable<IItemProperty> GetItemProperties(HashSet<string> propertyNames, Func<ItemPath, string> pathResolver)
    {
        var item = GetItem();
        if (item == null)
        {
            return Enumerable.Empty<IItemProperty>();
        }

        return item.ToPipelineObject(pathResolver).AsItemProperties().WherePropertiesMatch(propertyNames);
    }
}