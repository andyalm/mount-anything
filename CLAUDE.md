# CLAUDE.md - MountAnything

## Project Overview

MountAnything is a C# framework for building PowerShell providers that expose arbitrary APIs as hierarchical virtual filesystems. It allows navigating API objects using familiar `cd`, `ls`, `Get-Item`, `Get-ChildItems` PowerShell commands. The primary consumer of this framework is [MountAws](https://github.com/andyalm/mount-aws).

## Build & Test Commands

```bash
# Build the entire solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Run all tests
dotnet test

# Run tests for a specific project
dotnet test tests/MountAnything.Tests

# Publish locally (requires PowerShell, takes a version parameter)
# pwsh ./publish-local.ps1 -Version 1.0.0
```

## Project Structure

```
MountAnything.sln
├── src/
│   ├── MountAnything/                        # Core framework library (net6.0)
│   │   ├── Routing/                          # Router, Route, routing extensions
│   │   ├── Content/                          # Content read/write (streams)
│   │   ├── Item.cs                           # Base class for items returned to pipeline
│   │   ├── PathHandler.cs                    # Base class for handling path commands
│   │   ├── ItemPath.cs                       # Path abstraction (forward-slash normalized)
│   │   ├── Cache.cs                          # In-memory item/child-item cache
│   │   ├── Freshness.cs                      # Cache freshness strategies
│   │   ├── ProviderImpl.cs                   # Core provider implementation (IProviderImpl)
│   │   ├── IMountAnythingProvider.cs         # Main interface consumers implement
│   │   └── MountAnythingProvider.cs          # Base class with dynamic drive parameters
│   ├── MountAnything.Hosting.Abstractions/   # Minimal interfaces in global AssemblyLoadContext
│   │   ├── IProviderImpl.cs                  # Bridge interface for isolated assembly context
│   │   └── IProviderHost.cs                  # Host interface for PowerShell engine access
│   ├── MountAnything.Hosting.Build/          # MSBuild tasks for module generation (netstandard2.0)
│   └── MountAnything.Hosting.Templates/      # Template files for generated Provider/AssemblyLoadContext
├── tests/
│   └── MountAnything.Tests/                  # xUnit tests with FluentAssertions
├── examples/
│   └── mount-powershell/                     # Example provider navigating PowerShell modules/commands
└── .github/workflows/
    ├── ci.yml                                # CI: builds on all branches
    └── publish.yml                           # Publishes NuGet packages on GitHub release
```

## Key Abstractions

### Three Core Types

1. **Router** (`src/MountAnything/Routing/Router.cs`) - Maps URL-like paths to PathHandler types using regex-based routing. Created via `Router.Create<TRootHandler>()`. Supports `MapLiteral`, `Map<THandler>`, `Map<THandler, TTypedString>`, and `MapRegex`.

2. **PathHandler** (`src/MountAnything/PathHandler.cs`) - Abstract base class that processes commands for a matched path. Subclasses must implement:
   - `GetItemImpl()` - Returns the `IItem` at this path (or null)
   - `GetChildItemsImpl()` - Returns child items for `Get-ChildItems`

3. **Item<T>** (`src/MountAnything/Item.cs`) - Wraps an underlying .NET object for the PowerShell pipeline. Subclasses must implement:
   - `ItemName` - The virtual "filename" identifier
   - `IsContainer` - Whether it can have children

### Supporting Types

- **ItemPath** (`src/MountAnything/ItemPath.cs`) - Immutable path type using forward slashes. Normalizes backslashes, strips leading/trailing slashes.
- **Cache** (`src/MountAnything/Cache.cs`) - In-memory cache for items and child-item lists, keyed by path (case-insensitive).
- **Freshness** (`src/MountAnything/Freshness.cs`) - Cache staleness strategies: `Default`, `Guaranteed` (15s TTL), `Fastest` (4h TTL, for tab completion), `NoPartial`.
- **TypedString** (`src/MountAnything/TypedString.cs`) - Strongly-typed string base class for route-captured values injected via DI.
- **ItemNavigator** (`src/MountAnything/ItemNavigator.cs`) - Helper for flat-to-hierarchical item navigation.
- **LinkGenerator** (`src/MountAnything/LinkGenerator.cs`) - Constructs cross-references between items at different paths.

## Architecture Patterns

### Dependency Injection
- Uses **Autofac** as the DI container with Microsoft.Extensions.DependencyInjection integration.
- PathHandler constructors receive `ItemPath` and `IPathHandlerContext` plus any registered services.
- Route-captured values are registered as services and can be injected into handler constructors.
- `IItemAncestor<TItem>` allows injecting an ancestor item from higher in the path hierarchy.

### Assembly Load Context Isolation
- The hosting layer uses a separate `AssemblyLoadContext` to isolate provider assemblies from the PowerShell process.
- `MountAnything.Hosting.Abstractions` stays in the global context (minimal, stable interfaces).
- `MountAnything.Hosting.Templates/Provider.cs` is a template that gets code-generated into consumer projects at build time, bridging the PowerShell `NavigationCmdletProvider` to `IProviderImpl`.

### Optional Handler Interfaces
PathHandlers can implement additional interfaces to support more PowerShell commands:
- `INewItemHandler` - `New-Item`
- `IRemoveItemHandler` - `Remove-Item`
- `ISetItemHandler` - `Set-Item`
- `IClearItemHandler` - `Clear-Item`
- `IInvokeDefaultActionHandler` - `Invoke-Item`
- `IContentReaderHandler` / `IContentWriterHandler` - `Get-Content` / `Set-Content`
- `ISetItemPropertiesHandler`, `IClearItemPropertiesHandler`, etc. - Property commands
- Each command also has a corresponding `I*Parameters<T>` interface for dynamic parameters.

## Test Framework

- **xUnit** for test runner
- **FluentAssertions** for assertion syntax
- Tests cover: `ItemPath` operations, `Item` pipeline object behavior, routing resolution and handler creation, ancestor item injection

## CI/CD

- **CI** (`.github/workflows/ci.yml`): Runs `dotnet build` on every push (all branches). Uses .NET 6.0.
- **Publish** (`.github/workflows/publish.yml`): On GitHub release, builds and pushes `MountAnything` and `MountAnything.Hosting.Build` NuGet packages to nuget.org.

## Code Conventions

- **Target framework**: .NET 6.0 (netstandard2.0 for the Build project)
- **Nullable reference types**: Enabled across all projects
- **Implicit usings**: Enabled
- **File-scoped namespaces**: Used throughout (e.g., `namespace MountAnything;`)
- **NuGet packages**: Published with version derived from git tag (`PackageReleaseTag`)
- **Naming**: PascalCase for public members, `_camelCase` for private fields, standard C# conventions
- **No EditorConfig or formatting tools** configured - follow existing code style
