# Items

Items are the objects returned to the PowerShell pipeline by `Get-Item` and `Get-ChildItem`. They wrap an underlying .NET object and define how it appears in the virtual filesystem.

> **Prerequisite:** [Path Handlers](path-handlers.md)

## `Item<T>` — wrapping a typed object

The most common base class. The type parameter `T` is the .NET type of the object being wrapped:

```csharp
public class SecurityGroupItem : Item<SecurityGroup>
{
    public SecurityGroupItem(ItemPath parentPath, SecurityGroup sg) : base(parentPath, sg) { }

    public override string ItemName => UnderlyingObject.GroupId;
    public override bool IsContainer => false;
}
```

All public properties on `T` are automatically written to the PowerShell pipeline object.

## `Item` — wrapping a `PSObject`

A convenience subclass of `Item<PSObject>` for when you're working directly with PowerShell objects (e.g., results from `InvokeScript`):

```csharp
public class ModuleItem : Item
{
    public ModuleItem(ItemPath parentPath, PSObject underlyingObject) : base(parentPath, underlyingObject)
    {
        ItemName = underlyingObject.Property<string>("Name")!;
    }

    public override string ItemName { get; }
    public override bool IsContainer => true;
}
```

The non-generic `Item` also provides a helper `Property<T>(string name)` method for accessing properties on the underlying `PSObject`.

## `IItem` — lightweight interface

For simple cases (like a root node that doesn't wrap a real object), you can implement `IItem` directly:

```csharp
public class RootItem : IItem
{
    public ItemPath FullPath => ItemPath.Root;
    public bool IsContainer => true;

    public PSObject ToPipelineObject(Func<ItemPath, string> pathResolver)
    {
        return new PSObject();
    }
}
```

## Required members

| Member | Description |
|---|---|
| `ItemName` | The virtual "filename" that identifies this item. Prefer human-friendly names if they're guaranteed to be unique. |
| `IsContainer` | Whether this item can have children (determines if `cd` works on it). |

## Optional members

| Member | Default | Description |
|---|---|---|
| `IsPartial` | `false` | Marks the item as a partial representation (e.g., from a list API that returns fewer fields than a detail API). The cache uses this to decide when to refresh. |
| `ItemType` | `null` | A type string added to the pipeline object (e.g., `"Directory"`, `"File"`). |
| `TypeName` | The item class's full name | Controls the PowerShell type name on the pipeline object. |
| `Aliases` | Empty | Alternative names that resolve to this item (useful for IDs vs. display names). |
| `CustomizePSObject(PSObject)` | No-op | Hook to modify the pipeline object before it's returned. |

## Adding custom properties

### `[ItemProperty]` attribute

The simplest way to add properties to the pipeline object. Decorate a public property on your item class:

```csharp
public class ClusterItem : Item<Cluster>
{
    public ClusterItem(ItemPath parentPath, Cluster cluster) : base(parentPath, cluster) { }

    public override string ItemName => UnderlyingObject.Name;
    public override bool IsContainer => true;

    [ItemProperty]
    public bool HasRunningTasks => UnderlyingObject.RunningTasksCount > 0;
}
```

The optional `PropertyName` parameter lets you control the property name as it appears in PowerShell. Without it, the C# property name is used.

### `CustomizePSObject`

For more control, override `CustomizePSObject` to modify the `PSObject` directly:

```csharp
protected override void CustomizePSObject(PSObject psObject)
{
    psObject.Properties.Add(new PSNoteProperty("ComputedField", ComputeSomething()));
}
```

## Links and cross-references

Items can link to related items at different paths in the virtual filesystem. This is useful when an item logically relates to items elsewhere in the hierarchy.

### `Links`

A dictionary of named links to other `IItem` instances. The linked items are embedded as nested objects on the pipeline output, and their paths appear in the `Links` property:

```csharp
public class ServiceItem : Item<Service>
{
    public ServiceItem(ItemPath parentPath, Service service, TaskDefinitionItem taskDef)
        : base(parentPath, service)
    {
        Links = new Dictionary<string, IItem>
        {
            ["TaskDefinition"] = taskDef
        };
    }
}
```

### `LinkPaths`

A lighter-weight alternative that stores just the path without embedding the full item:

```csharp
public class ServiceItem : Item<Service>
{
    public ServiceItem(ItemPath parentPath, Service service, LinkGenerator linkGenerator)
        : base(parentPath, service)
    {
        LinkPaths = new Dictionary<string, ItemPath>
        {
            ["TaskDefinition"] = linkGenerator.ConstructPath(2, $"task-definitions/{service.TaskDefinition}")
        };
    }
}
```

Both `Links` and `LinkPaths` are surfaced through a `Links` property on the pipeline object, making cross-references navigable in PowerShell.

## Aliases

Items can declare alternative names that resolve to the same item. This is useful when resources have more than one way of uniquely identifying them:

```csharp
public override IEnumerable<string> Aliases => new[] { UnderlyingObject.AnotherId };
```

The cache stores entries for all aliases, so users can navigate by either name.

## See also

- [Path Handlers](path-handlers.md) — the handlers that return items
- [Caching](caching.md) — how `IsPartial` and aliases interact with the cache
- [Advanced Topics](advanced.md) — `LinkGenerator` for constructing cross-reference paths
