using System.Collections;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using Autofac;
using MountAnything.Content;
using MountAnything.Hosting.Abstractions;
using MountAnything.Routing;

namespace MountAnything;

public class ProviderImpl : IProviderImpl, IPathHandlerContext
{
    private readonly Assembly _entrypointAssembly;
    private readonly Cache _cache;
    private readonly IMountAnythingProvider _mountAnythingProvider;
    private readonly Router _router;

    private IProviderHost Host => ProviderHostAccessor.Current;

    public ProviderImpl(Assembly entrypointAssembly)
    {
        _entrypointAssembly = entrypointAssembly;
        _cache = new Cache();
        (_mountAnythingProvider, _router) = LoadRouter();
    }

    private (IMountAnythingProvider, Router) LoadRouter()
    {
        var routerFactoryType = _entrypointAssembly.GetExportedTypes()
            .SingleOrDefault(t => typeof(IMountAnythingProvider).IsAssignableFrom(t));
        if (routerFactoryType == null)
        {
            throw new InvalidOperationException(
                $"Could not find a public type that implements IMountAnythingProvider in assembly {_entrypointAssembly.FullName}");
        }

        var routerFactory = (IMountAnythingProvider)Activator.CreateInstance(routerFactoryType)!;
        return (routerFactory, routerFactory.CreateRouter());
    }

    private Router Router => _router;
    public Cache Cache => _cache;
    
    bool IPathHandlerContext.Force => Host.Force;
    CommandInvocationIntrinsics IPathHandlerContext.InvokeCommand => Host.InvokeCommand;
    PSDriveInfo IPathHandlerContext.DriveInfo => Host.PSDriveInfo;

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
        return GetDynamicParameters(path, typeof(IGetItemParameters<>));
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
        return GetDynamicParameters(path, typeof(IGetChildItemParameters<>));
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
                var newItem = newItemHandler.NewItem(itemTypeName, newItemValue);
                WriteItem(newItem);
            }
            else
            {
                throw new InvalidOperationException($"The powershell provider does not currently support creating this item");
            }
        });
    }

    public object? NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
    {
        return GetDynamicParameters(path, typeof(INewItemParameters<>));
    }

    public void RemoveItem(string path, bool recurse)
    {
        WriteDebug($"RemoveItem({path}, {recurse})");
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
        return GetDynamicParameters(path, typeof(IRemoveItemParameters<>));
    }

    public PSDriveInfo NewDrive(PSDriveInfo drive)
    {
        WriteDebug($"NewDrive({drive.Name}, {DynamicParameters?.GetType()?.FullName ?? "null"})");
        return _mountAnythingProvider.NewDrive(drive, DynamicParameters);
    }

    public object? NewDriveDynamicParameters()
    {
        return _mountAnythingProvider.CreateNewDriveDynamicParameters();
    }

    public PSDriveInfo RemoveDrive(PSDriveInfo drive)
    {
        return drive;
    }

    public Collection<PSDriveInfo> InitializeDefaultDrives()
    {
        var defaultDrives = _mountAnythingProvider
            .GetDefaultDrives(Host.ProviderInfo)
            .ToList();

        return new Collection<PSDriveInfo>(defaultDrives);
    }

    public void RenameItem(string path, string newName)
    {
        WriteDebug($"RenameItem({path}, {newName})");
        var parentPath = new ItemPath(path).Parent;
        var destination = parentPath.Combine(newName);
        MoveItem(path, destination.FullName);
        Cache.RemoveItem(parentPath);
    }

    public object? RenameItemDynamicParameters(string path, string newName)
    {
        return null;
    }

    public void CopyItem(string path, string copyPath, bool recurse)
    {
        WriteDebug($"CopyItem({path}, {copyPath}, {recurse})");
        WithPathHandler(path, sourceHandler =>
        {
            if (sourceHandler is IContentReaderHandler getContentHandler)
            {
                WithPathHandler(copyPath, destinationHandler =>
                {
                    if (destinationHandler is INewItemHandler newItemHandler && destinationHandler is IContentWriterHandler setContentHandler)
                    {
                        var sourceItem = sourceHandler.GetItem();
                        if (sourceItem == null)
                        {
                            throw new InvalidOperationException($"The item at {path} does not exist");
                        }
                        newItemHandler.NewItem(sourceItem.ItemType, null);
                        using var contentReader = getContentHandler.GetContentReader();
                        using var sourceStream = contentReader.GetContentStream();
                        var destinationWriter = setContentHandler.GetContentWriter();
                        using var destinationStream = destinationWriter.GetWriterStream();
                        sourceStream.CopyTo(destinationStream);
                        destinationWriter.WriterFinished(destinationStream);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "The powershell provider does not currently support coping this item");
                    }
                });
            }
            else
            {
                throw new InvalidOperationException($"The powershell provider does not currently support copying this item");
            }
        });
    }

    public object? CopyItemDynamicParameters(string path, string destination, bool recurse)
    {
        return null;
    }

    public void MoveItem(string path, string destination)
    {
        CopyItem(path, destination, true);
        RemoveItem(path, true);
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
        try
        {
            var itemPath = new ItemPath(path);
            var handlerResolver = Router.GetResolver(itemPath);

            return handlerResolver.CreateDynamicParameters(handlerParameterInterface);
        }
        catch (Exception ex)
        {
            WriteDebug(ex.ToString());
            return null;
        }
    }
    
    public string ToProviderPath(ItemPath path)
    {
        return $"{ItemSeparator}{path.FullName.Replace(ItemPath.Separator.ToString(), ItemSeparator.ToString())}";
    }

    public string ToFullyQualifiedProviderPath(ItemPath path)
    {
        return $"{Host.PSDriveInfo.Name}:{ToProviderPath(path)}";
    }
    
    public string NormalizeRelativePath(string path, string basePath)
    {
        var returnValue = Host.NormalizeRelativePathDefaultImpl(path, basePath);
        //HACK to make tab completion on top level directories work (I'm calling it a hack because I don't understand why its necessary)
        if (returnValue.StartsWith(ItemSeparator) && basePath == ItemSeparator.ToString())
        {
            returnValue = returnValue.Substring(1);
        }

        WriteDebug($"{returnValue} NormalizeRelativePath({path}, {basePath})");

        return returnValue;
    }

    public void GetChildNames(string path, ReturnContainers returnContainers)
    {
        WriteDebug($"GetChildNames({path}, {returnContainers})");
        Host.GetChildNamesDefaultImpl(path, returnContainers);
    }

    public object? GetChildNamesDynamicParameters(string path)
    {
        return null;
    }

    public void SetItem(string path, object value)
    {
        WriteDebug($"ClearItem({path})");
        WithPathHandler(path, handler =>
        {
            if (handler is ISetItemHandler setItemHandler)
            {
                handler.SetDynamicParameters(typeof(ISetItemParameters<>), DynamicParameters);
                setItemHandler.SetItem(value);
            }
            else
            {
                throw new InvalidOperationException($"The powershell provider does not currently support the Set-Item command against this item");
            }
        });
    }

    public object? SetItemDynamicParameters(string path, object value)
    {
        return GetDynamicParameters(path, typeof(ISetItemParameters<>));
    }

    public void ClearItem(string path)
    {
        WriteDebug($"ClearItem({path})");
        WithPathHandler(path, handler =>
        {
            if (handler is IClearItemHandler clearItemHandler)
            {
                handler.SetDynamicParameters(typeof(IClearItemParameters<>), DynamicParameters);
                clearItemHandler.ClearItem();
            }
            else
            {
                throw new InvalidOperationException($"The powershell provider does not currently support the Clear-Item command");
            }
        });
    }

    public object? ClearItemDynamicParameters(string path)
    {
        return GetDynamicParameters(path, typeof(IClearItemParameters<>));
    }

    public void InvokeDefaultAction(string path)
    {
        WriteDebug($"InvokeDefaultAction({path})");
        WithPathHandler(path, handler =>
        {
            if (handler is IInvokeDefaultActionHandler invokeHandler)
            {
                handler.SetDynamicParameters(typeof(IInvokeDefaultActionParameters<>), DynamicParameters);
                var resultingItems = invokeHandler.InvokeDefaultAction();
                if (resultingItems != null)
                {
                    WriteItems(resultingItems);
                }
            }
            else
            {
                throw new InvalidOperationException($"The powershell provider does not currently support invoking this item");
            }
        });
    }

    public object? InvokeDefaultActionDynamicParameters(string path)
    {
        return GetDynamicParameters(path, typeof(IInvokeDefaultActionParameters<>));
    }

    public string[] ExpandPath(string path)
    {
        WriteDebug($"ExpandPath({path})");
        var itemPath = new ItemPath(path);
        var handlerPath = itemPath.Parent;
        var pattern = itemPath.Name;
        var returnValue = WithPathHandler(handlerPath.FullName, handler =>
        {
            WriteDebug($"{handler.GetType().Name}.GetChildItems({pattern})");
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
        return Host.ConvertPathDefaultImpl(path, filter, ref updatedPath, ref updatedFilter);
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
            return new ContentReader(contentReadHandler.GetContentReader(), container, this);
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
        if (handler is IContentWriterHandler setContentHandler)
        {
            var writer = setContentHandler.GetContentWriter();
            return new ContentWriter(writer, container);
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
        WriteDebug($"ClearProperty({path})");
        WithPathHandler(path, handler =>
        {
            if (handler is IClearItemPropertiesHandler clearPropertyHandler)
            {
                handler.SetDynamicParameters(typeof(IClearItemPropertiesParameters<>), DynamicParameters);
                clearPropertyHandler.ClearItemProperties(propertyToClear);
            }
            else
            {
                throw new InvalidOperationException($"The powershell provider does not currently support clearing properties of this item");
            }
        });
    }

    public object? ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear)
    {
        return GetDynamicParameters(path, typeof(IClearItemPropertiesParameters<>));
    }

    public void GetProperty(string path, Collection<string> providerSpecificPickList)
    {
        WithPathHandler(path, handler =>
        {
            handler.SetDynamicParameters(typeof(IGetItemPropertiesParameters<>), DynamicParameters);
            var propertyNames = providerSpecificPickList.ToHashSet();
            var itemProperties = handler
                .GetItemProperties(propertyNames, ToFullyQualifiedProviderPath)
                .WherePropertiesMatch(propertyNames);
            var propertyObject = new Hashtable();
            foreach (var itemProperty in itemProperties)
            {
                propertyObject.Add(itemProperty.Name, itemProperty.Value);
            }

            WritePropertyObject(propertyObject, path);
        });
    }

    public object? GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList)
    {
        return GetDynamicParameters(path, typeof(IGetItemPropertiesParameters<>));
    }

    public void SetProperty(string path, PSObject propertyValue)
    {
        WriteDebug($"SetProperty({path}, <propertyValue>)");
        WithPathHandler(path, handler =>
        {
            if (handler is ISetItemPropertiesHandler setPropertyHandler)
            {
                handler.SetDynamicParameters(typeof(ISetItemPropertiesParameters<>), DynamicParameters);
                setPropertyHandler.SetItemProperties(propertyValue.AsItemProperties().ToList());
            }
            else
            {
                throw new InvalidOperationException($"The powershell provider does not currently support setting properties for this item");
            }
        });
    }

    public object? SetPropertyDynamicParameters(string path, PSObject propertyValue)
    {
        return GetDynamicParameters(path, typeof(ISetItemPropertiesParameters<>));
    }

    public void CopyProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty)
    {
        //TODO: Implement support
        throw NotSupported("Copy-ItemProperty");
    }

    public object? CopyPropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath,
        string destinationProperty)
    {
        //TODO: Implement support
        return null;
    }

    public void MoveProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty)
    {
        //TODO: Implement support
        throw NotSupported("Move-ItemProperty");
    }

    public object? MovePropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath,
        string destinationProperty)
    {
        //TODO: Implement support
        return null;
    }

    public void NewProperty(string path, string propertyName, string propertyTypeName, object value)
    {
        WriteDebug($"NewProperty({path}, {propertyName}, {propertyTypeName}, <propertyValue>)");
        WithPathHandler(path, handler =>
        {
            if (handler is INewItemPropertyHandler newPropertyHandler)
            {
                handler.SetDynamicParameters(typeof(INewItemPropertyParameters<>), DynamicParameters);
                newPropertyHandler.NewItemProperty(propertyName, propertyTypeName, value);
                WritePropertyObject(new Hashtable { [propertyName] = value }, path);
            }
            else
            {
                throw NotSupported("New-ItemProperty");
            }
        });
    }

    public object? NewPropertyDynamicParameters(string path, string propertyName, string propertyTypeName, object value)
    {
        return GetDynamicParameters(path, typeof(INewItemPropertyParameters<>));
    }

    public void RemoveProperty(string path, string propertyName)
    {
        WriteDebug($"RemoveProperty({path}, {propertyName})");
        WithPathHandler(path, handler =>
        {
            if (handler is IRemoveItemPropertyHandler newPropertyHandler)
            {
                handler.SetDynamicParameters(typeof(IRemoveItemPropertyParameters<>), DynamicParameters);
                newPropertyHandler.RemoveItemProperty(propertyName);
            }
            else
            {
                throw NotSupported("New-ItemProperty");
            }
        });
    }

    public object? RemovePropertyDynamicParameters(string path, string propertyName)
    {
        return GetDynamicParameters(path, typeof(IRemoveItemPropertyParameters<>));
    }

    public void RenameProperty(string path, string sourceProperty, string destinationProperty)
    {
        //TODO: Implement support
        throw NotSupported("Rename-ItemProperty");
    }

    public object? RenamePropertyDynamicParameters(string path, string sourceProperty, string destinationProperty)
    {
        //TODO: Implement support
        return null;
    }

    #endregion

    private void WriteError(ErrorRecord error) => Host.WriteError(error);
    void IPathHandlerContext.WriteWarning(string message) => WriteWarning(message);
    private void WriteWarning(string message) => Host.WriteWarning(message);

    void IPathHandlerContext.WriteDebug(string message) => WriteDebug(message);
    private void WriteDebug(string text) => Host.WriteDebug(text);

    private void WritePropertyObject(object propertyValue, string path) =>
        Host.WritePropertyObject(propertyValue, path);

    private void WriteItemObject(object item, string path, bool isContainer) =>
        Host.WriteItemObject(item, path, isContainer);

    private char ItemSeparator => Host.ItemSeparator;

    private object? DynamicParameters => Host.DynamicParameters;

    private string? Filter => Host.Filter;

    private Exception NotSupported(string commandName)
    {
        throw new PSNotSupportedException(
            $"The {commandName} command is not currently supported by this Powershell Provider.");
    }
}