namespace MountAnything;

public interface ICache
{
    void SetItem(IItem item);
    bool TryGetItem(ItemPath path, Freshness freshness, out IItem cachedItem);
    void SetChildItems(IItem item, IEnumerable<IItem> childItems);
    bool TryGetChildItems(ItemPath path, Freshness freshness, out IEnumerable<IItem> cachedChildItems);
    void RemoveItem(ItemPath path);
    string ResolveAlias<TItem>(string identifierOrAlias) where TItem : IItem;
    string ResolveAlias<TItem>(string identifierOrAlias, Func<string,string> findIdentifier) where TItem : IItem;
}