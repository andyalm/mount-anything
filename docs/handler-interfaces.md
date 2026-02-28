# Handler Interfaces

By default, `PathHandler` supports `Get-Item` and `Get-ChildItem`. To support additional PowerShell commands, implement the corresponding optional interface on your handler class.

> **Prerequisite:** [Path Handlers](path-handlers.md)

## Item operations

| Interface | PowerShell Command | Method |
|---|---|---|
| `INewItemHandler` | `New-Item` | `IItem NewItem(string? itemTypeName, object? newItemValue)` |
| `IRemoveItemHandler` | `Remove-Item` | `void RemoveItem()` |
| `ISetItemHandler` | `Set-Item` | `void SetItem(object value)` |
| `IClearItemHandler` | `Clear-Item` | `void ClearItem()` |
| `IInvokeDefaultActionHandler` | `Invoke-Item` | `IEnumerable<IItem>? InvokeDefaultAction()` |

### Example

```csharp
public class BucketObjectHandler : PathHandler, INewItemHandler, IRemoveItemHandler
{
    private readonly IS3Api _s3;

    public BucketObjectHandler(ItemPath path, IPathHandlerContext context, IS3Api s3)
        : base(path, context)
    {
        _s3 = s3;
    }

    protected override IItem? GetItemImpl() { /* ... */ }
    protected override IEnumerable<IItem> GetChildItemsImpl() { /* ... */ }

    public IItem NewItem(string? itemTypeName, object? newItemValue)
    {
        _s3.PutObject(ItemName, newItemValue?.ToString());
        Cache.RemoveItem(ParentPath);  // invalidate parent's child cache
        return GetItem()!;
    }

    public void RemoveItem()
    {
        _s3.DeleteObject(ItemName);
        Cache.RemoveItem(Path);
        Cache.RemoveItem(ParentPath);
    }
}
```

## Content operations

For `Get-Content` and `Set-Content` support, implement these interfaces on your handler:

| Interface | PowerShell Command | Method |
|---|---|---|
| `IContentReaderHandler` | `Get-Content` | `IStreamContentReader GetContentReader()` |
| `IContentWriterHandler` | `Set-Content` | `IStreamContentWriter GetContentWriter()` |

### `IStreamContentReader`

Return an object that provides a `Stream` for reading:

```csharp
public interface IStreamContentReader : IDisposable
{
    Stream GetContentStream();
}
```

Built-in implementations:
- `StreamContentReader` — wraps any `Stream`
- `HttpResponseContentReader` — wraps an `HttpResponseMessage`
- `EmptyContentReader` — returns an empty stream

### `IStreamContentWriter`

Return an object that provides a writable `Stream` and a callback for when writing is complete:

```csharp
public interface IStreamContentWriter
{
    Stream GetWriterStream();
    void WriterFinished(Stream stream);
}
```

Built-in implementation:
- `StreamContentWriter` — writes to a `MemoryStream` and calls your callback with the result

### Example

```csharp
public class FileHandler : PathHandler, IContentReaderHandler, IContentWriterHandler
{
    public IStreamContentReader GetContentReader()
    {
        var stream = FetchFileContent();
        return new StreamContentReader(stream);
    }

    public IStreamContentWriter GetContentWriter()
    {
        return new StreamContentWriter(stream =>
        {
            UploadFileContent(stream);
            Cache.RemoveItem(Path);
        });
    }
}
```

## Item property operations

| Interface | PowerShell Command | Method |
|---|---|---|
| `ISetItemPropertiesHandler` | `Set-ItemProperty` | `void SetItemProperties(PSObject properties)` |
| `IClearItemPropertiesHandler` | `Clear-ItemProperty` | `void ClearItemProperties(IEnumerable<string> propertyNames)` |
| `INewItemPropertyHandler` | `New-ItemProperty` | `void NewItemProperty(string name, string? typeName, object value)` |
| `IRemoveItemPropertyHandler` | `Remove-ItemProperty` | `void RemoveItemProperty(string name)` |

## Dynamic parameters

Each command has a corresponding dynamic parameters interface that lets you add custom PowerShell parameters:

| Interface | For Command |
|---|---|
| `IGetItemParameters<T>` | `Get-Item` |
| `IGetChildItemParameters<T>` | `Get-ChildItem` |
| `INewItemParameters<T>` | `New-Item` |
| `IRemoveItemParameters<T>` | `Remove-Item` |
| `ISetItemParameters<T>` | `Set-Item` |
| `IClearItemParameters<T>` | `Clear-Item` |
| `IInvokeDefaultActionParameters<T>` | `Invoke-Item` |
| `IGetItemPropertiesParameters<T>` | `Get-ItemProperty` |
| `ISetItemPropertiesParameters<T>` | `Set-ItemProperty` |
| `IClearItemPropertiesParameters<T>` | `Clear-ItemProperty` |
| `INewItemPropertyParameters<T>` | `New-ItemProperty` |
| `IRemoveItemPropertyParameters<T>` | `Remove-ItemProperty` |

The type parameter `T` is a class whose properties are decorated with `[Parameter]` attributes. These become additional parameters on the PowerShell command when the provider is active.

## See also

- [Path Handlers](path-handlers.md) — the base handler class
- [Caching](caching.md) — invalidating cache after write operations
