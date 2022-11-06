namespace MountAnything;

public class Cache
{
    private readonly Dictionary<string, CachedItem> _itemsByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<(Type ItemType, string Alias), string> _itemIdsByAliases = new();

    public void SetItem(IItem item)
    {
        foreach (var path in item.CacheablePaths)
        {
            if(_itemsByPath.TryGetValue(path.FullName, out var cachedItem))
            {
                cachedItem.Item = item;
            }
            else
            {
                _itemsByPath[path.FullName] = new CachedItem(item);
            }
        }

        _itemIdsByAliases[(item.GetType(), item.ItemName)] = item.ItemName;
        foreach (var alias in item.Aliases)
        {
            _itemIdsByAliases[(item.GetType(), alias.ToLower())] = item.ItemName;
        }

        foreach (var linkedItem in item.Links.Values)
        {
            SetItem(linkedItem);
        }
    }

    public bool TryGetItem(ItemPath path, out (IItem Item, DateTimeOffset FreshnessTimestamp) cachedObject)
    {
        if (_itemsByPath.TryGetValue(path.FullName, out var cachedItem))
        {
            cachedObject = (cachedItem.Item, cachedItem.FreshnessTimestamp);
            return true;
        }

        cachedObject = default!;
        return false;
    }
    
    public void SetChildItems(IItem item, IEnumerable<IItem> childItems)
    {
        SetItem(item);
        foreach (var childItem in childItems)
        {
            SetItem(childItem);
        }
        var cachedItem = _itemsByPath[item.FullPath.FullName];
        cachedItem.ChildPaths = childItems.Select(i => i.FullPath).ToList();
    }

    public bool TryGetChildItems(ItemPath path, out (IEnumerable<IItem> ChildItems, DateTimeOffset FreshnessTimestamp) cachedObject)
    {
        if (_itemsByPath.TryGetValue(path.FullName, out var cachedItem) && cachedItem.ChildPaths != null)
        {
            var childItems = cachedItem.ChildPaths.Select(childPath => _itemsByPath[childPath.FullName].Item).ToArray();
            cachedObject = (childItems, cachedItem.FreshnessTimestamp);
            return true;
        }

        cachedObject = default!;
        return false;
    }

    public void RemoveItem(ItemPath path)
    {
        _itemsByPath.Remove(path.FullName);
    }

    public string ResolveAlias<TItem>(string identifierOrAlias) where TItem : IItem
    {
        if(_itemIdsByAliases.TryGetValue((typeof(TItem),identifierOrAlias), out var identifier))
        {
            return identifier;
        }

        return identifierOrAlias;
    }
    
    public string ResolveAlias<TItem>(string identifierOrAlias, Func<string,string> findIdentifier) where TItem : IItem
    {
        if(_itemIdsByAliases.TryGetValue((typeof(TItem),identifierOrAlias), out var identifier))
        {
            return identifier;
        }

        var resolvedIdentifier = findIdentifier(identifierOrAlias);
        _itemIdsByAliases[(typeof(TItem), identifierOrAlias)] = resolvedIdentifier;

        return resolvedIdentifier;
    }
    
    private class CachedItem
    {
        private IItem _item;

        public CachedItem(IItem item)
        {
            _item = item;
            FreshnessTimestamp = DateTimeOffset.UtcNow;
            ChildPaths = null;
        }

        public IItem Item
        {
            get => _item;
            set
            {
                _item = value;
                FreshnessTimestamp = DateTimeOffset.UtcNow;
            }
        }
        public List<ItemPath>? ChildPaths { get; set; }
        public DateTimeOffset FreshnessTimestamp { get; private set; }
    }
}