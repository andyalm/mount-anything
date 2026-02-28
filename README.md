# MountAnything

A framework for building PowerShell providers that expose arbitrary APIs as hierarchical virtual filesystems. Navigate any API with familiar commands like `cd`, `ls`, `Get-Item`, and `Get-Content`.

Some example providers built with this framework include:

- [MountAws](https://github.com/andyalm/mount-aws) - AWS services
- [MountGitlab](https://github.com/andyalm/mount-gitlab) - Gitlab
- [MountConsul](https://github.com/andyalm/mount-consul) - Consul KV store
- [MountVault](https://github.com/andyalm/mount-vault) - HashiCorp Vault
- [MountArtifactory](https://github.com/andyalm/mount-artifactory) - JFrog Artifactory

## What it looks like

```powershell
# Navigate AWS ECS resources as a filesystem
cd aws:\us-east-1\ecs\clusters\my-cluster\services
ls

# Inspect a specific resource
Get-Item my-service
```

## Quick start

1. Reference the NuGet packages:

```xml
<PackageReference Include="MountAnything" Version="*" />
<PackageReference Include="MountAnything.Hosting.Build" Version="*" />
```

2. Implement `IMountAnythingProvider` to define your virtual filesystem's route structure:

```csharp
public class MyProvider : IMountAnythingProvider
{
    public Router CreateRouter()
    {
        var router = Router.Create<RootHandler>();
        router.MapLiteral<ModulesHandler>("modules", modules =>
        {
            modules.Map<ModuleHandler>();
        });
        return router;
    }
}
```

3. Implement `PathHandler` subclasses that return `Item` objects for each path.

4. Build your project — the `MountAnything.Hosting.Build` package auto-generates a PowerShell module in your output directory. Import it and start navigating.

See the [Getting Started guide](docs/getting-started.md) for a full walkthrough with the included example project.

## Key concepts

- **[Router](docs/routing.md)** — Maps URL-like paths to handlers using composable route definitions (`MapLiteral`, `Map<T>`, `MapRegex`).
- **[PathHandler](docs/path-handlers.md)** — Processes `Get-Item` and `Get-ChildItem` for a matched path. Supports automatic caching, dependency injection, and optional write operations.
- **[Item](docs/items.md)** — Wraps a .NET object for the PowerShell pipeline. Defines the virtual filename and whether the item can have children.

## Documentation

- [Getting Started](docs/getting-started.md) — step-by-step guide
- [Routing](docs/routing.md) — all `Map` variants and route hierarchy
- [Path Handlers](docs/path-handlers.md) — handler context, optional overrides, constructor injection
- [Items](docs/items.md) — custom properties, aliases, links
- [Caching](docs/caching.md) — freshness strategies and cache control
- [Dependency Injection](docs/dependency-injection.md) — service registration, typed captures, ancestor items
- [Handler Interfaces](docs/handler-interfaces.md) — supporting `New-Item`, `Remove-Item`, `Get-Content`, etc.
- [Advanced Topics](docs/advanced.md) — `ItemPath`, `LinkGenerator`, `ItemNavigator`, assembly isolation

## Building from source

```bash
dotnet build
dotnet test
```

## License

See [LICENSE](LICENSE).
