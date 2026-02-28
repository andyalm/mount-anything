# Path Handlers

A `PathHandler` processes PowerShell provider commands (`Get-Item`, `Get-ChildItem`, etc.) for a matched path. When the [Router](routing.md) matches a path, it resolves the corresponding handler and delegates the command to it.

> **Prerequisite:** [Routing](routing.md)

## The `PathHandler` base class

Inherit from `PathHandler` rather than implementing `IPathHandler` directly. The base class provides automatic caching, debug logging, and convenience properties:

```csharp
public class ModuleHandler : PathHandler
{
    public ModuleHandler(ItemPath path, IPathHandlerContext context) : base(path, context) { }

    protected override IItem? GetItemImpl()
    {
        var module = Context.InvokeCommand
            .InvokeScript($"Get-Module -Name {ItemName} -ErrorAction SilentlyContinue")
            .SingleOrDefault();

        return module != null ? new ModuleItem(ParentPath, module) : null;
    }

    protected override IEnumerable<IItem> GetChildItemsImpl()
    {
        return Context.InvokeCommand.InvokeScript($"Get-Command -Module {ItemName}")
            .Select(c => new CommandItem(Path, c));
    }
}
```

## Required methods

| Method | Called by | Purpose |
|---|---|---|
| `GetItemImpl()` | `Get-Item` | Return the item at this path, or `null` if it doesn't exist |
| `GetChildItemsImpl()` | `Get-ChildItem` | Return all child items of this path |

## Optional overrides

| Member | Default | Purpose |
|---|---|---|
| `ExistsImpl()` | Calls `GetItem(Freshness.Fastest)` | Override for a more efficient existence check |
| `GetChildItems(string filter)` | Calls `GetChildItemsImpl()` then filters | Override to support efficient server-side filtering (used by tab completion and `-Filter`) |
| `CacheChildren` | `true` | Set to `false` if child items are too numerous to cache |
| `GetItemCommandDefaultFreshness` | `Freshness.Guaranteed` | Controls when `Get-Item` uses cached results |
| `GetChildItemsCommandDefaultFreshness` | `Freshness.Guaranteed` | Controls when `Get-ChildItem` uses cached results |

See [Caching](caching.md) for details on freshness strategies.

## Available properties

These properties are available in any `PathHandler` subclass:

| Property | Type | Description |
|---|---|---|
| `Path` | `ItemPath` | The full path being handled |
| `ParentPath` | `ItemPath` | The parent of the current path |
| `ItemName` | `string` | The last segment of the path (the "filename") |
| `Context` | `IPathHandlerContext` | Access to cache, PowerShell engine, and debug logging |
| `Cache` | `Cache` | Shorthand for `Context.Cache` |
| `LinkGenerator` | `LinkGenerator` | Helper for constructing cross-reference paths |

## `IPathHandlerContext`

The context provides access to the PowerShell engine and provider state:

| Member | Type | Description |
|---|---|---|
| `Cache` | `Cache` | The in-memory item cache |
| `WriteDebug(string)` | — | Write a debug message (visible with `-Debug` flag) |
| `WriteWarning(string)` | — | Write a warning message |
| `Force` | `bool` | Whether the `-Force` flag was specified |
| `InvokeCommand` | `CommandInvocationIntrinsics` | Execute PowerShell commands from within a handler |
| `DriveInfo` | `PSDriveInfo` | The current PSDrive (useful for accessing drive-level configuration) |

### Calling PowerShell from handlers

Use `Context.InvokeCommand.InvokeScript()` to execute PowerShell commands and get results as `PSObject` collections:

```csharp
protected override IEnumerable<IItem> GetChildItemsImpl()
{
    return Context.InvokeCommand.InvokeScript("Get-Module")
        .Select(m => new ModuleItem(Path, m));
}
```

## Constructor injection

Handler constructors support dependency injection. Besides the required `ItemPath` and `IPathHandlerContext`, you can inject any service registered with the router:

```csharp
public class ServiceHandler : PathHandler
{
    private readonly IItemAncestor<ClusterItem> _cluster;
    private readonly IEcsApi _ecs;

    public ServiceHandler(ItemPath path, IPathHandlerContext context,
        IItemAncestor<ClusterItem> cluster, IEcsApi ecs) : base(path, context)
    {
        _cluster = cluster;
        _ecs = ecs;
    }
}
```

See [Dependency Injection](dependency-injection.md) for details on service registration, route captures, and ancestor item injection.

## Write operations

By default, handlers only support read operations (`Get-Item`, `Get-ChildItem`). To support commands like `New-Item`, `Remove-Item`, `Get-Content`, etc., implement optional handler interfaces. See [Handler Interfaces](handler-interfaces.md).

## See also

- [Routing](routing.md) — how paths are matched to handlers
- [Items](items.md) — the objects handlers return
- [Caching](caching.md) — controlling cache freshness
- [Dependency Injection](dependency-injection.md) — constructor injection
- [Handler Interfaces](handler-interfaces.md) — write operations
