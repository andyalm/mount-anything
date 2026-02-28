# Caching

MountAnything includes an in-memory cache that makes tab completion fast and reduces redundant API calls. The `PathHandler` base class integrates with the cache automatically — you generally don't need to interact with it directly, but understanding how it works helps you tune performance.

> **Prerequisite:** [Path Handlers](path-handlers.md)

## How it works

When a `PathHandler` calls `GetItem()` or `GetChildItems()`, the base class checks the cache before calling your `GetItemImpl()` or `GetChildItemsImpl()` methods. If a fresh cached result exists, your implementation is skipped entirely.

Items are cached by path (case-insensitive). Child item lists are cached as a set of paths associated with the parent item.

## Freshness strategies

The `Freshness` class determines whether a cached item is "fresh enough" to use. There are four built-in strategies:

| Strategy | Behavior | Use case |
|---|---|---|
| `Freshness.Default` | Uses cache unless `-Force` is specified | Standard reads |
| `Freshness.Guaranteed` | Uses cache only if it's less than 15 seconds old and `-Force` is not set | The default for `Get-Item` and `Get-ChildItem` commands |
| `Freshness.Fastest` | Uses cache if it's less than 4 hours old, ignoring `-Force` | Tab completion and path expansion, where speed matters more than freshness |
| `Freshness.NoPartial` | Uses cache only if the item is not marked as partial | `GetItem()` default — ensures full item data is fetched when a list API returned partial data |

## Controlling cache behavior

### `GetItemCommandDefaultFreshness` and `GetChildItemsCommandDefaultFreshness`

Override these properties to change when the cache is used for `Get-Item` and `Get-ChildItem` commands:

```csharp
public class ExpensiveHandler : PathHandler
{
    // Allow cached results for up to 15 seconds
    public override Freshness GetItemCommandDefaultFreshness => Freshness.Guaranteed;

    // Use cache aggressively for child items
    public override Freshness GetChildItemsCommandDefaultFreshness => Freshness.Default;
}
```

Both default to `Freshness.Guaranteed` (15-second TTL).

### `CacheChildren`

By default, child item paths are cached when `GetChildItemsImpl()` returns. This enables fast tab completion. Override this property to `false` if the number of children is very large and caching them would waste memory:

```csharp
protected override bool CacheChildren => false;
```

### Partial items (`IsPartial`)

Many APIs return less data in list operations than in detail operations. Mark list-derived items as partial so the cache knows to re-fetch when full data is needed:

```csharp
public class ServiceItem : Item<Service>
{
    private readonly bool _isPartial;

    public ServiceItem(ItemPath parentPath, Service service, bool isPartial = false)
        : base(parentPath, service)
    {
        _isPartial = isPartial;
    }

    public override bool IsPartial => _isPartial;
    // ...
}
```

When `GetItem()` is called with `Freshness.NoPartial` (the default), a partial cached item is treated as a cache miss, causing `GetItemImpl()` to be called to fetch the full item.

## Cache invalidation

After write operations (e.g., `New-Item`, `Remove-Item`), you may need to invalidate cached entries:

```csharp
Cache.RemoveItem(path);
```

This removes the item and its associated child list from the cache.

## Alias resolution

The cache supports alias resolution via `Cache.ResolveAlias<TItem>(string identifierOrAlias)`. If an item declares aliases (see [Items](items.md)), both the primary name and aliases are stored in the cache, and `ResolveAlias` maps aliases back to the canonical name.

## See also

- [Path Handlers](path-handlers.md) — freshness overrides and `CacheChildren`
- [Items](items.md) — `IsPartial` and `Aliases`
