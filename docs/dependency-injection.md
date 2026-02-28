# Dependency Injection

MountAnything uses [Autofac](https://autofac.readthedocs.io/) as its DI container. All `PathHandler` instances are resolved through DI, so you can inject services, route-captured values, and ancestor items into handler constructors.

> **Prerequisite:** [Routing](routing.md), [Path Handlers](path-handlers.md)

## Registering services

### Top-level registration

Register services on the `Router` that are available to all handlers:

```csharp
public Router CreateRouter()
{
    var router = Router.Create<RootHandler>();

    // Microsoft.Extensions.DependencyInjection style
    router.ConfigureServices(services =>
    {
        services.AddSingleton<IMyApi, MyApi>();
    });

    // Autofac-native style (for advanced scenarios)
    router.ConfigureContainer(builder =>
    {
        builder.Register(_ => RegionEndpoint.UsEast1);
    });

    return router;
}
```

### Route-scoped registration

Register services at a specific point in the route hierarchy. These override top-level registrations for handlers at this level and below:

```csharp
router.Map<RegionHandler>("Region", region =>
{
    region.ConfigureServices((services, match) =>
    {
        // Override the default region with the one from the URL
        services.AddTransient(_ => RegionEndpoint.FromSystemName(match.Values["Region"]));
    });
});
```

The `match` parameter provides access to captured route values via `match.Values["name"]`.

**Hierarchy rule:** A registration at a child route overrides the same service registered at a parent route. For example:

```csharp
// Default: us-east-1 for all handlers
router.ConfigureContainer(builder => builder.Register(_ => RegionEndpoint.UsEast1));

router.Map<RegionHandler>("Region", region =>
{
    // Override: use the region from the path for handlers under /regions/*
    region.ConfigureServices((services, match) =>
    {
        services.AddTransient(_ => RegionEndpoint.FromSystemName(match.Values["Region"]));
    });
});
```

## Route captures as injectable services

Named captures from routing are automatically available for DI. When you use `Map<T>("Name", ...)`, the captured string is registered under that name.

### Named string captures

```csharp
router.Map<RegionHandler>("Region", region => { ... });
```

Any handler at this level or below can receive the captured value. However, since it's registered as a plain `string`, this can be ambiguous when multiple captures exist.

### `TypedString` captures

For type-safe injection, use `Map<THandler, TTypedString>` which registers a strongly-typed wrapper:

```csharp
public class Cluster : TypedString
{
    public Cluster(string value) : base(value) { }
}

// In the router:
clusters.Map<ClusterHandler, Cluster>(cluster => { ... });
```

Now handlers can inject `Cluster` directly:

```csharp
public class ServiceHandler : PathHandler
{
    private readonly Cluster _cluster;

    public ServiceHandler(ItemPath path, IPathHandlerContext context, Cluster cluster)
        : base(path, context)
    {
        _cluster = cluster;
    }
}
```

`TypedString` has an implicit conversion to `string`, so you can use it wherever a string is expected.

### `TypedItemPath` captures

For `MapRecursive`, the captured multi-segment path is wrapped in a `TypedItemPath`:

```csharp
public class S3Key : TypedItemPath
{
    public S3Key(ItemPath path) : base(path) { }
}

router.MapRecursive<S3ObjectHandler, S3Key>();
```

## Injecting ancestor items

Sometimes a handler needs context from an item higher in the path hierarchy. Use `IItemAncestor<TItem>` to inject it:

```csharp
public class EcsServiceHandler : PathHandler
{
    private readonly IItemAncestor<ClusterItem> _cluster;
    private readonly IEcsApi _ecs;

    public EcsServiceHandler(ItemPath path, IPathHandlerContext context,
        IItemAncestor<ClusterItem> cluster, IEcsApi ecs) : base(path, context)
    {
        _cluster = cluster;
        _ecs = ecs;
    }

    protected override IItem? GetItemImpl()
    {
        var service = _ecs.DescribeService(
            serviceName: ItemName,
            clusterName: _cluster.Item.ItemName);

        return new EcsServiceItem(ParentPath, service);
    }
}
```

**How it works:** When `IItemAncestor<ClusterItem>` is resolved, the `ItemAncestorResolver` walks up the path hierarchy, calling `GetItem()` on each ancestor handler until it finds one that returns an item of type `ClusterItem`.

This requires that a handler higher in the routing hierarchy returns a `ClusterItem` from its `GetItemImpl()` method.

## What's automatically registered

The following services are always available for injection:

| Service | Description |
|---|---|
| `ItemPath` | The current path being handled |
| `IPathHandlerContext` | The handler context (cache, debug logging, etc.) |
| `Router` | The router instance |
| `IItemAncestor<T>` | Ancestor item resolver (for any `T : IItem`) |

Any concrete class not explicitly registered is also resolved automatically (via Autofac's `AnyConcreteTypeNotAlreadyRegisteredSource`), so handler classes don't need manual registration.

## See also

- [Routing](routing.md) — how captures are defined
- [Path Handlers](path-handlers.md) — constructor injection
- [Advanced Topics](advanced.md) — `TypedString` and `TypedItemPath` details
