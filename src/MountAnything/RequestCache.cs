using System.Management.Automation;

namespace MountAnything;

public class RequestCache : ICache
{
    private readonly Cache _requestCache = new();
    private readonly ICache _longTermCache;
    private readonly bool _force;

    public RequestCache(ICache longTermCache, bool force)
    {
        _longTermCache = longTermCache;
        _force = force;
    }

    public void SetItem(IItem item)
    {
        _requestCache.SetItem(item);
        _longTermCache.SetItem(item);
    }

    public bool TryGetItem(ItemPath path, Freshness freshness, out IItem cachedItem)
    {
        if (_requestCache.TryGetItem(path, freshness, out cachedItem))
        {
            return true;
        }

        return !_force && _longTermCache.TryGetItem(path, freshness, out cachedItem);
    }

    public void SetChildItems(IItem item, IEnumerable<IItem> childItems)
    {
        _longTermCache.SetChildItems(item, childItems);
    }

    public bool TryGetChildItems(ItemPath path, Freshness freshness, out IEnumerable<IItem> cachedChildItems)
    {
        cachedChildItems = default!;
        return !_force && _longTermCache.TryGetChildItems(path, freshness, out cachedChildItems);
    }

    public void RemoveItem(ItemPath path)
    {
        _requestCache.RemoveItem(path);
        _longTermCache.RemoveItem(path);
    }

    public string ResolveAlias<TItem>(string identifierOrAlias) where TItem : IItem
    {
        return _longTermCache.ResolveAlias<TItem>(identifierOrAlias);
    }

    public string ResolveAlias<TItem>(string identifierOrAlias, Func<string, string> findIdentifier) where TItem : IItem
    {
        return _longTermCache.ResolveAlias<TItem>(identifierOrAlias, findIdentifier);
    }
}

public class RequestCachePathHandlerContext : IPathHandlerContext
{
    private readonly IPathHandlerContext _inner;

    public RequestCachePathHandlerContext(IPathHandlerContext inner)
    {
        _inner = inner;
        Cache = new RequestCache(inner.Cache, inner.Force);
    }

    public ICache Cache { get; }
    public void WriteDebug(string message) => _inner.WriteDebug(message);
    public void WriteWarning(string message) => _inner.WriteWarning(message);
    public bool Force => _inner.Force;
    public CommandInvocationIntrinsics InvokeCommand => _inner.InvokeCommand;
    public PSCredential? Credential => _inner.Credential;
}