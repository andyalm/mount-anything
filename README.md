# MountAnything

A framework for building powershell providers to make it easy to navigate arbitrary API's as a hierarchical virtual filesystem of objects.

## Getting started

1. Reference the `MountAnything` nuget package in your csproj project that will contain your powershell provider.
2. Create a class that inherits from `MountAnythingProvider`.
3. Implement the `CreateRouter` method. For information on creating a router, see the [Router](#Router) section below.

## Key abstractions

There are three key abstractions that drive MountAnything. The `Router`, `PathHandler`'s, and `Item`'s:

### Router

Every path in the virtual filesystem is processed by the `Router` to determine which `PathHandler` will process the command.
The Router API composes a nested hierarchy of routes. Under the hood, routes are regex based, but you usually can use a more convenient
extension method to avoid needing to actually deal with regex's. Here is an example of the routing api from the [MountAws](https://github.com/andyalm/mount-aws) project:

```c#
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
            clusters.Map<ClusterHandler,Cluster>(cluster =>
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

In the example, you can see a few different variations of `Map` methods used. All of them take a generic type argument that corresponds to the `IPathHandler` that will be invoked for matching routes. They are:

* `MapLiteral` - This matches on the literal string (e.g. constant) passed into it. Only that literal string will match the route.
* `Map<THandler>` - This matches any supported character (pretty much anything besides a `/`, which is used as the path separator) at this hierarchy level. You can optionally pass in a string as the first argument to this method if you would like to capture the value of the matched value. The captured value will be given the name that is passed as the argument. The captured value can be used for dependency injection into the `PathHander` of this or any child route.
* `Map<THandler,TCapture>` - This is similar to the `Map` above, except it contains a second type parameter that is a [TypedString](src/MountAnything/TypedString.cs) whose value will be populated from the matched route value and can be injected into the constructor of this or any child `PathHandler`.
* `MapRegex` - This is the lower level method that the above two methods call under the hood. Any regex is acceptable, so long as it does not contain the `^` or `$` characters for declaring the beginning or end of a string. Those are implicitly added by the router as necessary. It is important to note that any regex you are adding is implicitly concatenated with the regex's built by parent and child routes when the router is matching. Named captures are allowed in the regex and those captured values can  be used for dependency injection into the `PathHandler` of this or any child route.

### PathHandler

The `PathHandler` is in charge of processing a command to the powershell provider.
While there is an `IPathHandler`, it is expected that 99% of the time you will want to use
the `PathHandler` abstract base class instead for convenience. It will automatically handle
things like caching for you, which helps make things like tab completion as performant as possible.

The `PathHandler` base class has only two methods that you are required to implement:

* `GetItemImpl` - This is called when the `Get-Item` command is called. It should return the `IItem` that corresponds to the path that this `PathHandler` is processing. If no item exists at this path, it should return `null`.
* `GetChildItemsImpl` - This is called when the `Get-ChildItems` command. Its also used to support tab completion by default. It should return all of the child items of the item returned by the `GetItemImpl` method.

In addition, you can optionally override the following methods when helpful/necessary:

* `ExistsImpl` - By default, existence is checked by calling `GetItem` and determining if it returned `null` or not. However, if you can provide a more performant/optimal implementation, you can override this method.
* `GetChildItems(string filter)` - This method supports tab completion, as well as when the `-Filter` argument is used on the `Get-ChildItems` command. By default, the `GetChildItemsImpl` method is called and the filter as applied to entire set of items returned. However, if you can provide a more performant implementation that does not require fetching all items first, you are encouraged to do so by overriding this method.
* `CacheChildren` - By default, the paths of the child items returned by `GetChildItemsImpl` are cached to help make things like tab completion faster. However, if there are potentially a very large number of child items for this handler, you may want to tell it not to do this by overriding this property and returning `false`.
* `GetItemCommandDefaultFreshness` - This allows you to customize when the cache is used for `Get-Item` commands.
* `GetChildItemsCommandDefaultFreshness` - This allows you to customize when the cache is used for `Get-ChildItems` commands.

### Item

This represents the object/item that is returned to the console by `Get-Item` and `Get-ChildItems` commands. It is generally a wrapper
class around an underlying object that will be sent to the console. There is a generic version of `Item<T>` where the type
argument represents the .net type of the item that will be sent to the console. If you inherit from the non-generic `Item`, the
underlying type will be a `PSObject`. Either way, all properties on the underlying type will be written to the powershell pipeline. The
`Item` class has a couple methods that need to be implemented in the subclass to tell the powershell provider what the path of the item is:

* `ItemName` - This identifies the virtual "filename" of the item. It should be something that naturally identifies the item. Prefer human friendly names if they are guaranteed to be unique.
* `IsContainer` - This indicates whether this could have child items or not.

Here is an example of a simple `Item` implementation:

```c#
public class SecurityGroupItem : Item<SecurityGroup>
{
    public SecurityGroupItem(ItemPath parentPath, SecurityGroup securityGroup) : base(parentPath, securityGroup) {}

    public override string ItemName => UnderlyingObject.GroupId;
    
    public override bool IsContainer => false;
}
```

## Dependency Injection

All `IPathHandler` instances support dependency injection, powered by [Autofac](https://autofac.readthedocs.io/).
The Router provides a `RegisterServices` method that allows you to use Autofac's [ContainerBuilder](https://autofac.readthedocs.io/en/latest/register/registration.html)
to register additional services that can be injected into your `PathHandler`'s. Services can be registered at any point in the routing
hierarchy and a registration further down in the hierarchy will override one that happens higher up. For example, take this example:

```c#
// registers the default implementation of RegionEndpoint to be us-east-1
router.RegisterServices(builder => builder.Register(_ => RegionEndpoint.UsEast1));
router.MapLiteral<RegionsHandler>("regions", regions =>
{
    regions.Map<RegionHandler>("Region", region =>
    {
        region.RegisterServices((match, builder) =>
        {
            // overrides the default region registration above
            builder.Register(_ => RegionEndpoint.FromSystemName(match.Values["Region"]);
        });
    });
});
```

In the above example, any PathHandler underneath the `/regions` path will be injected the region from the current path. Any PathHandler
outside of the `/regions` path will have the `us-east-1` region injected.

### Injecting an ancestor item

Sometimes `PathHandler`'s need to know something about a specific item above them in the path hierarchy. You can have an ancestor item
injected into your `PathHandler`'s constructor by using the `IItemAncestor<TItem>` interface. For example, in this theoretical example,
an EcsService handler wants to know what ECS cluster it belongs to, so it declares `IItemAncestor<ClusterItem>` as a constructor dependency:

```c#
public class EcsServiceHandler : PathHandler
{
    private readonly IItemAncestor<ClusterItem> _cluster;
    private readonly IEcsApi _ecs;

    public EcsServiceHandler(ItemPath path, IPathHandlerContext context, IItemAncestor<ClusterItem> cluster, IEcsApi ecs) : base(path, context)
    {
        _cluster = cluster;
        _ecs = ecs;
    }
    
    protected override IItem GetItemImpl()
    {
        var ecsService = _ecs.DescribeService(serviceName: ItemName, clusterName: _cluster.Name);
        
        return new EcsServiceItem(ParentPath, ecsService);
    }
}
```

This example assumes there is a `IPathHandler` higher in the routing hierarchy whose `GetItem` implementation returns an item of type `ClusterItem`.
The `IItemAncestor<TItem>` implementation walks up the hierarchy looking for an item whose type matches the one declared as `TItem`.