# Routing

The `Router` maps virtual filesystem paths to `PathHandler` types using a composable, regex-based routing system. Every path that a user navigates to is matched against the router to determine which handler processes the command.

## Creating a router

Create a router in your `IMountAnythingProvider.CreateRouter()` method. The root handler is the handler invoked when the user is at the drive root:

```csharp
public Router CreateRouter()
{
    var router = Router.Create<RootHandler>();
    // define child routes here
    return router;
}
```

## Map methods

Routes form a nested hierarchy. At each level, you choose a mapping method that determines how a path segment is matched.

### `MapLiteral`

Matches an exact, case-insensitive string:

```csharp
router.MapLiteral<ModulesHandler>("modules", modules =>
{
    // child routes under /modules/...
});
```

Only the literal string `modules` will match. This is ideal for fixed navigation nodes like `ecs`, `s3`, `modules`, etc.

### `Map<THandler>`

Matches any path segment using the pattern `[a-z0-9-_.:]+`:

```csharp
modules.Map<ModuleHandler>(module =>
{
    // child routes under /modules/<any-module-name>/...
});
```

This is the most common way to match dynamic path segments (resource names, IDs, etc.).

### `Map<THandler>` with named capture

Captures the matched value under a name so it can be injected into handler constructors via dependency injection:

```csharp
router.Map<RegionHandler>("Region", region =>
{
    // "Region" captured value is now available for DI
});
```

The captured value is registered as a `string` keyed by the name `"Region"`. Any `PathHandler` at this level or below can receive it through constructor injection.

### `Map<THandler, TTypedString>`

Like named capture, but wraps the value in a strongly-typed `TypedString` subclass:

```csharp
router.Map<ClusterHandler, Cluster>(cluster =>
{
    // Cluster typed string is available for DI
});
```

Where `Cluster` is:

```csharp
public class Cluster : TypedString
{
    public Cluster(string value) : base(value) { }
}
```

The type name is used as the capture name. This gives you type safety when injecting route values — you can distinguish between a `Cluster` and a `ServiceName` even though both are strings. See [Dependency Injection](dependency-injection.md) for more on `TypedString`.

### `MapRegex`

The low-level method that all other `Map` methods call under the hood. Accepts any regex pattern:

```csharp
router.MapRegex<RegionHandler>("(?<Region>[a-z0-9-]+)", region =>
{
    // child routes
});
```

**Important:** Do not include `^` or `$` anchors — they are added automatically by the router. Named capture groups in the regex are available for dependency injection.

### `MapRecursive`

Matches a multi-segment path (including `/` separators), useful for tree structures like file paths or hierarchical keys:

```csharp
router.MapRecursive<S3ObjectHandler, S3Key>();
```

Where `S3Key` extends `TypedItemPath`:

```csharp
public class S3Key : TypedItemPath
{
    public S3Key(ItemPath path) : base(path) { }
}
```

The entire remaining path is captured as a `TypedItemPath` and injected into the handler.

## How routes compose

Routes are hierarchically composed. When a child route is defined inside a parent route, the parent's regex pattern is prepended with a `/` separator:

```csharp
router.MapLiteral<ClustersHandler>("clusters", clusters =>
{
    clusters.Map<ClusterHandler>();  // matches: clusters/[a-z0-9-_.:]+
});
```

The router tries child routes first (most specific match), then falls back to the parent. This means deeper, more specific routes take priority.

## Full example

From the [MountAws](https://github.com/andyalm/mount-aws) project:

```csharp
router.MapRegex<RegionHandler>("(?<Region>[a-z0-9-]+)", region =>
{
    region.MapLiteral<EcsRootHandler>("ecs", ecs =>
    {
        ecs.MapLiteral<TaskDefinitionsHandler>("task-definitions", taskDefinitions =>
        {
            taskDefinitions.Map<TaskDefinitionHandler>();
        });
        ecs.MapLiteral<ClustersHandler>("clusters", clusters =>
        {
            clusters.Map<ClusterHandler, Cluster>(cluster =>
            {
                cluster.MapLiteral<ServicesHandler>("services", services =>
                {
                    services.Map<ServiceHandler>();
                });
            });
        });
    });
});
```

This creates a filesystem like:

```
/us-east-1/ecs/task-definitions/my-task
/us-east-1/ecs/clusters/my-cluster/services/my-service
```

## Route-level service registration

You can register services at any point in the routing hierarchy. See [Dependency Injection](dependency-injection.md) for details.

## See also

- [Getting Started](getting-started.md) — creating your first provider
- [Path Handlers](path-handlers.md) — what handlers do once a route matches
- [Dependency Injection](dependency-injection.md) — registering services and injecting route captures
