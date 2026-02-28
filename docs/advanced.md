# Advanced Topics

This page covers utility types and architectural details for advanced use cases.

## `ItemPath`

`ItemPath` is an immutable path type used throughout MountAnything. It normalizes backslashes to forward slashes and strips leading/trailing separators.

```csharp
var path = new ItemPath(@"us-east-1\ecs\clusters");
// path.FullName == "us-east-1/ecs/clusters"
```

### Key members

| Member | Description |
|---|---|
| `FullName` | The normalized path string |
| `Name` | The last segment (e.g., `"clusters"`) |
| `Parent` | The parent path (e.g., `"us-east-1/ecs"`) |
| `Parts` | Array of path segments |
| `IsRoot` | Whether this is the empty root path |
| `Combine(parts)` | Append segments to the path |
| `Ancestor(name)` | Walk up to find an ancestor with the given name |
| `IsAncestorOf(path)` | Check if this path is an ancestor of another |
| `MatchesPattern(pattern)` | Wildcard matching (supports `*`) |

### Constants and conversions

```csharp
ItemPath.Root          // The empty root path
ItemPath.Separator     // '/'

// Explicit casts (not implicit, to avoid accidental conversions)
var path = (ItemPath)"some/path";
var str = (string)path;
```

## `LinkGenerator`

`LinkGenerator` helps construct paths to items elsewhere in the hierarchy. It's available as a property on every `PathHandler`.

```csharp
// Construct a path using the first N parts of the current path as a base
var taskDefPath = LinkGenerator.ConstructPath(
    numberOfParentPathParts: 2,    // e.g., "us-east-1/ecs"
    childPath: $"task-definitions/{taskDefName}"
);
// Result: "us-east-1/ecs/task-definitions/my-task-def"
```

This is useful for creating cross-references in item `LinkPaths`:

```csharp
public class ServiceItem : Item<Service>
{
    public ServiceItem(ItemPath parentPath, Service service, LinkGenerator linkGenerator)
        : base(parentPath, service)
    {
        LinkPaths = new Dictionary<string, ItemPath>
        {
            ["TaskDefinition"] = linkGenerator.ConstructPath(2, $"task-definitions/{service.TaskDef}")
        };
    }
}
```

## `ItemNavigator<TModel, TItem>`

`ItemNavigator` converts a flat list of items with hierarchical paths into a directory-like structure. This is useful when an API returns a flat list (e.g., S3 object keys) that you want to present as nested directories.

Subclass `ItemNavigator<TModel, TItem>` and implement:

| Method | Purpose |
|---|---|
| `CreateDirectoryItem(parentPath, directoryPath)` | Create a virtual directory item |
| `CreateItem(parentPath, model)` | Create a leaf item from a model |
| `GetPath(model)` | Extract the hierarchical path from a model |
| `ListItems(pathPrefix)` | Fetch all models (optionally filtered by prefix) |

Then call `ListChildItems(parentPath)` to get the items for a given level:

```csharp
public class S3Navigator : ItemNavigator<S3Object, IItem>
{
    protected override IItem CreateDirectoryItem(ItemPath parentPath, ItemPath dirPath)
        => new S3DirectoryItem(parentPath, dirPath.Name);

    protected override IItem CreateItem(ItemPath parentPath, S3Object obj)
        => new S3ObjectItem(parentPath, obj);

    protected override ItemPath GetPath(S3Object obj)
        => new ItemPath(obj.Key);

    protected override IEnumerable<S3Object> ListItems(ItemPath? pathPrefix)
        => _s3.ListObjects(prefix: pathPrefix?.FullName);
}
```

## `MountAnythingProvider<TDriveParameters>`

For providers that need custom parameters on `New-PSDrive`, inherit from `MountAnythingProvider<T>` instead of implementing `IMountAnythingProvider` directly:

```csharp
public class MyDriveParameters
{
    [Parameter(Mandatory = true)]
    public string Profile { get; set; } = "";

    [Parameter]
    public string Region { get; set; } = "us-east-1";
}

public class MyProvider : MountAnythingProvider<MyDriveParameters>
{
    public override Router CreateRouter() { /* ... */ }

    protected override PSDriveInfo NewDrive(PSDriveInfo driveInfo, MyDriveParameters parameters)
    {
        return new MyPsDriveInfo(driveInfo, parameters.Profile, parameters.Region);
    }
}
```

Users can then pass custom parameters when mounting:

```powershell
New-PSDrive -Name aws -PSProvider MyProvider -Root '' -Profile production -Region eu-west-1
```

## Assembly Load Context isolation

MountAnything uses .NET's `AssemblyLoadContext` to isolate provider assemblies from the PowerShell host process. This prevents version conflicts between provider dependencies and PowerShell's own dependencies.

The architecture:

- **`MountAnything.Hosting.Abstractions`** stays in the global (default) `AssemblyLoadContext`. It contains only two small interfaces (`IProviderImpl` and `IProviderHost`) to minimize coupling.
- **`MountAnything`** and your provider assembly are loaded into an isolated `AssemblyLoadContext` created at module import time.
- **`MountAnything.Hosting.Build`** generates a bridge class at build time (from the template in `MountAnything.Hosting.Templates`) that lives in the global context and delegates to `IProviderImpl` across the isolation boundary.

This means your provider can use any version of any NuGet package without conflicting with other providers or PowerShell itself.

## See also

- [Items](items.md) — `Links`, `LinkPaths`, and `Aliases`
- [Routing](routing.md) — `MapRecursive` for hierarchical paths
- [Dependency Injection](dependency-injection.md) — `TypedString` and `TypedItemPath`
