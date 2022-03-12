using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;
using Autofac;
using MountAnything.Content;
using MountAnything.Hosting.Abstractions;
using MountAnything.Routing;

namespace MountAnything;

public class ProviderImpl : IProviderImpl
{
    // We need cache and router to be long-lived instances. Since powershell creates instances
    // of a provider for what appears to be every command, these must be stored as statics so that
    // we can re-use instances across command invocations. Since this is a base class, all inheriting providers
    // will be sharing the static instances, so we need to store an instance of each per provider module to ensure
    // state does not leak between providers.
    private static readonly ConcurrentDictionary<string,Cache> _caches = new();
    private static readonly ConcurrentDictionary<string,Router> _routers = new();
    
    private readonly Lazy<Cache> _cache;
    private readonly Lazy<Router> _router;
    
    
    public ProviderImpl()
    {
        _cache = new Lazy<Cache>(() => _caches.GetOrAdd(StaticCacheKey, _ => new Cache()));
        _router = new Lazy<Router>(() => _routers.GetOrAdd(StaticCacheKey, _ => LoadRouterViaIsolatedContext()));
    }

    private string StaticCacheKey => $"{GetType().FullName}";

    private Router Router => _router.Value;
    public Cache Cache => _cache.Value;
    
    bool IPathHandlerContext.Force => base.Force.IsPresent;

    public ProviderInfo Start(ProviderInfo providerInfo)
    {
        return providerInfo;
    }

    public object? StartDynamicParameters()
    {
        return null;
    }

    public void Stop() { }
    
    public bool IsValidPath(string path) => true;
    public bool ItemExists(string path)
    {
        //WriteDebug($"ItemExists({path})");
        if (path.Contains("*"))
        {
            return false;
        }

        try
        {
            return WithPathHandler(path, handler => handler.Exists());
        }
        catch (Exception ex)
        {
            WriteDebug(ex.ToString());
            throw;
        }
    }

    public object? ItemExistsDynamicParameters(string path)
    {
        return null;
    }

    public void GetItem(string path)
    {
        WriteDebug($"GetItem({path})");
        try
        {
            WithPathHandler(path, handler =>
            {
                handler.SetDynamicParameters(typeof(IGetItemParameters<>), DynamicParameters);
                var item = handler.GetItem(handler.GetItemCommandDefaultFreshness);
                if (item != null)
                {
                    WriteItem(item);
                }
                else
                {
                    WriteError(new ErrorRecord(new ApplicationException(
                        $"Cannot find item with path '{path}' because it does not exist"),
                        "404", ErrorCategory.ObjectNotFound, null));
                }
            });
        }
        catch (Exception ex)
        {
            WriteDebug(ex.ToString());
            throw;
        }
    }

    public object? GetItemDynamicParameters(string path)
    {
        try
        {
            return GetDynamicParameters(path, typeof(IGetItemParameters<>));
        }
        catch (Exception ex)
        {
            WriteDebug(ex.ToString());
            return null;
        }
    }

    public void GetChildItems(string path, bool recurse)
    {
        WriteDebug($"GetChildItems({path}, {recurse})");
        WithPathHandler(path, handler =>
        {
            handler.SetDynamicParameters(typeof(IGetChildItemParameters<>), DynamicParameters);
            var childItems = string.IsNullOrEmpty(Filter)
                ? handler.GetChildItems(handler.GetChildItemsCommandDefaultFreshness)
                : handler.GetChildItems(Filter);
            WriteItems(childItems);
        });
    }

    public object? GetChildItemsDynamicParameters(string path, bool recurse)
    {
        try
        {
            return GetDynamicParameters(path, typeof(IGetChildItemParameters<>));
        }
        catch (Exception ex)
        {
            WriteDebug(ex.ToString());
            return null;
        }
    }

    public void GetChildItems(string path, bool recurse, uint depth)
    {
        GetChildItems(path, recurse);
    }

    public bool HasChildItems(string path)
    {
        WriteDebug($"HasChildItems({path})");
        return WithPathHandler<bool?>(path, handler => handler.GetChildItems(Freshness.Fastest).Any()) ?? false;
    }

    public bool IsItemContainer(string path)
    {
        WriteDebug($"IsItemContainer({path})");
        return WithPathHandler(path, handler => handler.GetItem(Freshness.Fastest)?.IsContainer) ?? false;
    }

    public void NewItem(string path, string itemTypeName, object? newItemValue)
    {
        WithPathHandler(path, handler =>
        {
            if (handler is INewItemHandler newItemHandler)
            {
                handler.SetDynamicParameters(typeof(INewItemParameters<>), DynamicParameters);
                newItemHandler.NewItem(itemTypeName, newItemValue);
            }
            else
            {
                throw new InvalidOperationException($"The powershell provider does not currently support creating this item");
            }
        });
    }

    public object? NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
    {
        try
        {
            return GetDynamicParameters(path, typeof(INewItemParameters<>));
        }
        catch (Exception ex)
        {
            WriteDebug(ex.ToString());
            return null;
        }
    }

    public void RemoveItem(string path, bool recurse)
    {
        WithPathHandler(path, handler =>
        {
            if (handler is IRemoveItemHandler removeItemHandler)
            {
                handler.SetDynamicParameters(typeof(IRemoveItemParameters<>), DynamicParameters);
                removeItemHandler.RemoveItem();
            }
            else
            {
                throw new InvalidOperationException($"The powershell provider does not currently support removing this item");
            }
        });
    }

    public object? RemoveItemDynamicParameters(string path, bool recurse)
    {
        try
        {
            return GetDynamicParameters(path, typeof(IRemoveItemParameters<>));
        }
        catch (Exception ex)
        {
            WriteDebug(ex.ToString());
            return null;
        }
    }

    public PSDriveInfo NewDrive(PSDriveInfo drive)
    {
        return drive;
    }

    public object? NewDriveDynamicParameters()
    {
        return null;
    }

    public PSDriveInfo RemoveDrive(PSDriveInfo drive)
    {
        return drive;
    }

    public Collection<PSDriveInfo> InitializeDefaultDrives()
    {
        return new Collection<PSDriveInfo>();
    }

    public void RenameItem(string path, string newName)
    {
        throw NotImplemented();
    }

    public object? RenameItemDynamicParameters(string path, string newName)
    {
        return null;
    }

    public void CopyItem(string path, string copyPath, bool recurse)
    {
        throw NotImplemented();
    }

    public object? CopyItemDynamicParameters(string path, string destination, bool recurse)
    {
        return null;
    }

    public void MoveItem(string path, string destination)
    {
        throw NotImplemented();
    }

    public object? MoveItemDynamicParameters(string path, string destination)
    {
        return null;
    }
    
    

    private void WriteItems<T>(IEnumerable<T> items) where T : IItem
    {
        foreach (var item in items)
        {
            WriteItem(item);
        }
    }
    
    private void WriteItem(IItem item)
    {
        Cache.SetItem(item);
        var providerPath = ToProviderPath(item.FullPath);
        var pipelineObject = item.ToPipelineObject(ToFullyQualifiedProviderPath);
        WriteDebug($"WriteItemObject<{pipelineObject.TypeNames.First()}>({providerPath},{item.IsContainer})");
        WriteItemObject(pipelineObject, providerPath, item.IsContainer);
    }

    private (IPathHandler Handler, ILifetimeScope Container) GetPathHandler(string path)
    {
        return Router.RouteToHandler(new ItemPath(path), this);
    }

    private TReturn? WithPathHandler<TReturn>(string path, Func<IPathHandler,TReturn> action)
    {
        try
        {
            var (handler, container) = GetPathHandler(path);
            using (container)
            {
                return action(handler);
            }
        }
        catch (RoutingException ex)
        {
            WriteDebug($"RoutingException: {ex.Message}");
            return default;
        }
        catch (Exception ex)
        {
            WriteDebug(ex.ToString());
            WriteError(new ErrorRecord(ex, "2", ErrorCategory.NotSpecified, this));
            throw;
        }
    }
    
    private void WithPathHandler(string path, Action<IPathHandler> action)
    {
        WithPathHandler<object?>(path, handler =>
        {
            action(handler);

            return null;
        });
    }
    
    private object? GetDynamicParameters(string path, Type handlerParameterInterface)
    {
        var itemPath = new ItemPath(path);
        var handlerResolver = Router.GetResolver(itemPath);

        return handlerResolver.CreateDynamicParameters(handlerParameterInterface);
    }
    
    public string ToProviderPath(ItemPath path)
    {
        return $"{ItemSeparator}{path.FullName.Replace(ItemPath.Separator.ToString(), ItemSeparator.ToString())}";
    }

    public string ToFullyQualifiedProviderPath(ItemPath path)
    {
        return $"{PSDriveInfo.Name}:{ToProviderPath(path)}";
    }

    public void GetChildNames(string path, ReturnContainers returnContainers)
    {
        WriteDebug($"GetChildNames({path}, {returnContainers})");
        base.GetChildNames(path, returnContainers);
    }

    public object? GetChildNamesDynamicParameters(string path)
    {
        return null;
    }

    public void SetItem(string path, object value)
    {
        throw NotImplemented();
    }

    public object? SetItemDynamicParameters(string path, object value)
    {
        return null;
    }

    public void ClearItem(string path)
    {
        throw NotImplemented();
    }

    public object? ClearItemDynamicParameters(string path)
    {
        return null;
    }

    public void InvokeDefaultAction(string path)
    {
        throw NotImplemented();
    }

    public object? InvokeDefaultActionDynamicParameters(string path)
    {
        return null;
    }

    public string[] ExpandPath(string path)
    {
        WriteDebug($"ExpandPath({path})");
        var itemPath = new ItemPath(path);
        var handlerPath = itemPath.Parent;
        var pattern = itemPath.Name;
        var returnValue = WithPathHandler(handlerPath.FullName, handler =>
        {
            WriteDebug($"{handler.GetType().Name}.ExpandPath({pattern})");
            return handler.GetChildItems(pattern)
                .Select(i => i.MatchingCacheablePath(itemPath))
                .Select(p => p.Parts.Length == 1 ? ToProviderPath(p).Substring(1) : ToProviderPath(p))
                .ToArray();
        }) ?? Array.Empty<string>();
        foreach (var expandedPath in returnValue)
        {
            WriteDebug($"  {expandedPath}");
        }

        return returnValue;
    }

    public bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter)
    {
        WriteDebug($"ConvertPath({path}, {filter})");
        return base.ConvertPath(path, filter, ref updatedPath, ref updatedFilter);
    }

    #region Content

    public void ClearContent(string path)
    {
        using var contentWriter = GetContentWriter(path);
        contentWriter.Write(new ArrayList());
        contentWriter.Close();
    }

    public object? ClearContentDynamicParameters(string path)
    {
        return null;
    }

    public IContentReader GetContentReader(string path)
    {
        var (handler, container) = GetPathHandler(path);
        if (handler is IContentReaderHandler contentReadHandler)
        {
            return new HandlerDisposingProxy(container, contentReadHandler.GetContentReader());
        }

        container.Dispose();
        throw new InvalidOperationException("This item does not support reading content");
    }

    public object? GetContentReaderDynamicParameters(string path)
    {
        return null;
    }

    public IContentWriter GetContentWriter(string path)
    {
        var (handler, container) = GetPathHandler(path);
        if (handler is IContentWriterHandler contentWriteHandler)
        {
            return new HandlerDisposingProxy(container, contentWriteHandler.GetContentWriter());
        }

        container.Dispose();
        throw new InvalidOperationException("This item does not support writing content");
    }

    public object? GetContentWriterDynamicParameters(string path)
    {
        return null;
    }

    #endregion

    #region Properties

    public void ClearProperty(string path, Collection<string> propertyToClear)
    {
        throw new NotSupportedException("Only reading properties is currently supported by this provider");
    }

    public object? ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear)
    {
        return null;
    }

    public void GetProperty(string path, Collection<string> providerSpecificPickList)
    {
        WithPathHandler(path, handler =>
        {
            var propertyNames = providerSpecificPickList.ToHashSet();
            var itemProperties = handler
                .GetItemProperties(propertyNames, ToFullyQualifiedProviderPath)
                .WherePropertiesMatch(propertyNames);
            var propertyObject = new PSObject();
            foreach (var itemProperty in itemProperties)
            {
                propertyObject.Properties.Add(new PSNoteProperty(itemProperty.Name, itemProperty.Value));
            }
            WritePropertyObject(propertyObject, path);
        });
    }

    public object? GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList)
    {
        return null;
    }

    public void SetProperty(string path, PSObject propertyValue)
    {
        throw new NotSupportedException("Only reading properties is currently supported by this provider");
    }

    public object? SetPropertyDynamicParameters(string path, PSObject propertyValue)
    {
        return null;
    }

    #endregion
    
    

    private void WriteDebug(string text)
    {
        throw new NotImplementedException();
    }

    private void WritePropertyObject(object propertyValue, string path)
    {
        throw new NotImplementedException();
    }

    private string ItemSeparator => throw new NotImplementedException();
    
    private Exception NotImplemented()
    {
        throw new PSNotImplementedException("This operation is not currently supported by this provider");
    }
}