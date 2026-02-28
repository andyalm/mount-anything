# Getting Started

MountAnything is a framework for building PowerShell providers that expose arbitrary APIs as hierarchical virtual filesystems. This guide walks through creating a provider from scratch, using the included `mount-powershell` example as a reference.

> **Prerequisites:** .NET 6.0+ SDK, PowerShell 7+

## 1. Create a project and add NuGet references

Create a new class library project and reference the two required packages:

```xml
<PackageReference Include="MountAnything" Version="*" />
<PackageReference Include="MountAnything.Hosting.Build" Version="*" />
```

`MountAnything` is the core framework. `MountAnything.Hosting.Build` is an MSBuild integration that auto-generates the PowerShell module files at build time.

## 2. Implement `IMountAnythingProvider`

Create a class implementing `IMountAnythingProvider`. The only required method is `CreateRouter()`, which defines the virtual filesystem's URL-like path structure:

```csharp
using MountAnything;
using MountAnything.Routing;

public class MountPowershellProvider : IMountAnythingProvider
{
    public Router CreateRouter()
    {
        var router = Router.Create<RootHandler>();
        router.MapLiteral<ModulesHandler>("modules", modules =>
        {
            modules.Map<ModuleHandler>(module =>
            {
                module.Map<CommandHandler>();
            });
        });
        router.MapLiteral<CommandsHandler>("commands", commands =>
        {
            commands.Map<CommandHandler>();
        });
        return router;
    }

    public IEnumerable<DefaultDrive> GetDefaultDrives()
    {
        yield return new DefaultDrive("pwsh")
        {
            Description = "Navigate powershell objects as a hierarchical virtual drive"
        };
    }
}
```

`GetDefaultDrives` is optional — it automatically mounts a PSDrive when the module is imported. Without it, users must call `New-PSDrive` manually.

## 3. Implement path handlers

Each path in the virtual filesystem is handled by a `PathHandler` subclass. You need to implement two methods:

- `GetItemImpl()` — returns the item at this path (or `null` if it doesn't exist)
- `GetChildItemsImpl()` — returns the child items listed by `Get-ChildItem`

Here's a handler that lists PowerShell modules:

```csharp
using MountAnything;

public class ModulesHandler : PathHandler
{
    public ModulesHandler(ItemPath path, IPathHandlerContext context) : base(path, context) { }

    protected override IItem? GetItemImpl()
    {
        return new GenericContainerItem(ParentPath, "modules");
    }

    protected override IEnumerable<IItem> GetChildItemsImpl()
    {
        return Context.InvokeCommand.InvokeScript("Get-Module")
            .Select(m => new ModuleItem(Path, m));
    }
}
```

And a handler for an individual module:

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

See [Path Handlers](path-handlers.md) for details on optional overrides like caching control and filter support.

## 4. Implement items

Items represent the objects returned to the PowerShell pipeline. Subclass `Item<T>` (or `Item` for `PSObject`-backed items) and provide:

- `ItemName` — the virtual "filename" that identifies this item
- `IsContainer` — whether this item can have children

```csharp
using System.Management.Automation;
using MountAnything;

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

See [Items](items.md) for details on custom properties, aliases, links, and the `[ItemProperty]` attribute.

## 5. Build and test

Build the project:

```bash
dotnet build
```

The `MountAnything.Hosting.Build` package generates a PowerShell module in `bin/Debug/net6.0/Module/` (or whichever target framework you're using). This includes a `.psd1` manifest and the compiled provider DLL.

Import and test the module:

```powershell
Import-Module ./bin/Debug/net6.0/Module/MountPowershell.psd1

# If you implemented GetDefaultDrives, the drive is already mounted:
cd pwsh:

# Otherwise, mount it manually:
New-PSDrive -Name pwsh -PSProvider MountPowershell -Root ''

# Navigate the virtual filesystem
cd pwsh:\modules
Get-ChildItem
Get-Item Microsoft.PowerShell.Utility
cd Microsoft.PowerShell.Utility
Get-ChildItem   # lists commands in the module
```

## Next steps

- [Routing](routing.md) — all `Map` variants and route configuration
- [Path Handlers](path-handlers.md) — optional overrides and the handler context
- [Items](items.md) — custom properties, aliases, and links
- [Caching](caching.md) — freshness strategies and cache control
- [Dependency Injection](dependency-injection.md) — registering services and injecting ancestor items
- [Handler Interfaces](handler-interfaces.md) — supporting write operations (`New-Item`, `Remove-Item`, etc.)
- [Advanced Topics](advanced.md) — `ItemPath`, `LinkGenerator`, `ItemNavigator`, and more
