using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using MountAnything.Hosting.Abstractions;

namespace MountAnything.Build;

[CmdletProvider("MyProviderName", ProviderCapabilities.Filter | ProviderCapabilities.ExpandWildcards)]
public partial class Provider : NavigationCmdletProvider,
    IContentCmdletProvider,
    IPropertyCmdletProvider
{
    private readonly Lazy<IProviderImpl> _provider;
    
    private IProviderImpl LoadProviderImplIsolatedContext()
    {
        var assembly = LoadImplAssembly();
        var providerImplType = assembly
            .GetExportedTypes()
            .Single(t => typeof(IProviderImpl).IsAssignableFrom(t));

        return (IProviderImpl)Activator.CreateInstance(providerImplType)!;
    }

    private Assembly LoadImplAssembly()
    {
        var modulePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var apiAssemblyDir = Path.Combine(modulePath, "Impl");
        var assemblyLoadContext = new ProviderAssemblyContext(apiAssemblyDir);
        
        return assemblyLoadContext.LoadFromAssemblyName(new AssemblyName("MountAnything.Impl"));
    }
    
    protected override bool IsValidPath(string path) => _provider.Value.ItemExists(path);
    protected override bool ItemExists(string path)
    {
        return _provider.Value.ItemExists(path);
    }

    protected override object? ItemExistsDynamicParameters(string path)
    {
        return _provider.Value.ItemExistsDynamicParameters(path);
    }

    protected override void GetItem(string path)
    {
        _provider.Value.GetItem(path);
    }

    protected override object? GetItemDynamicParameters(string path)
    {
        return _provider.Value.GetItemDynamicParameters(path);
    }
    
    protected override void GetChildItems(string path, bool recurse, uint depth)
    {
        _provider.Value.GetChildItems(path, recurse, depth);
    }

    protected override object? GetChildItemsDynamicParameters(string path, bool recurse)
    {
        return _provider.Value.GetChildItemsDynamicParameters(path, recurse);
    }
    
    protected override bool HasChildItems(string path)
    {
        return _provider.Value.HasChildItems(path);
    }

    protected override bool IsItemContainer(string path)
    {
        return _provider.Value.IsItemContainer(path);
    }

    protected override void NewItem(string path, string itemTypeName, object? newItemValue)
    {
        _provider.Value.NewItem(path, itemTypeName, newItemValue);
    }

    protected override object? NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
    {
        return _provider.Value.NewItemDynamicParameters(path, itemTypeName, newItemValue);
    }

    protected override void RemoveItem(string path, bool recurse)
    {
        _provider.Value.RemoveItem(path, recurse);
    }

    protected override object? RemoveItemDynamicParameters(string path, bool recurse)
    {
        return _provider.Value.RemoveItemDynamicParameters(path, recurse);
    }

    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        _provider.Value.GetChildNames(path, returnContainers);
    }

    protected override object? GetChildNamesDynamicParameters(string path)
    {
        return _provider.Value.GetChildNamesDynamicParameters(path);
    }

    protected override string[] ExpandPath(string path)
    {
        return _provider.Value.ExpandPath(path);
    }

    protected override bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter)
    {
        return _provider.Value.ConvertPath(path, filter, ref updatedPath, ref updatedFilter);
    }

    protected override string NormalizeRelativePath(string path, string basePath)
    {
        var returnValue = base.NormalizeRelativePath(path, basePath);
        //HACK to make tab completion on top level directories work (I'm calling it a hack because I don't understand why its necessary)
        if (returnValue.StartsWith(ItemSeparator) && basePath == ItemSeparator.ToString())
        {
            returnValue = returnValue.Substring(1);
        }

        WriteDebug($"{returnValue} NormalizeRelativePath({path}, {basePath})");

        return returnValue;
    }

    protected override ProviderInfo Start(ProviderInfo providerInfo)
    {
        return _provider.Value.Start(providerInfo);
    }

    protected override object? StartDynamicParameters()
    {
        return _provider.Value.StartDynamicParameters();
    }

    protected override void Stop()
    {
        _provider.Value.Stop();
    }

    protected override void CopyItem(string path, string copyPath, bool recurse)
    {
        _provider.Value.CopyItem(path, copyPath, recurse);
    }

    protected override object? CopyItemDynamicParameters(string path, string destination, bool recurse)
    {
        return _provider.Value.CopyItemDynamicParameters(path, destination, recurse);
    }

    protected override void MoveItem(string path, string destination)
    {
        _provider.Value.MoveItem(path, destination);
    }

    protected override object? MoveItemDynamicParameters(string path, string destination)
    {
        return _provider.Value.MoveItemDynamicParameters(path, destination);
    }

    protected override void RenameItem(string path, string newName)
    {
        _provider.Value.RenameItem(path, newName);
    }

    protected override object? RenameItemDynamicParameters(string path, string newName)
    {
        return _provider.Value.RenameItemDynamicParameters(path, newName);
    }

    protected override void ClearItem(string path)
    {
        _provider.Value.ClearItem(path);
    }

    protected override object? ClearItemDynamicParameters(string path)
    {
        return _provider.Value.ClearItemDynamicParameters(path);
    }

    protected override void SetItem(string path, object value)
    {
        _provider.Value.SetItem(path, value);
    }

    protected override object? SetItemDynamicParameters(string path, object value)
    {
        return _provider.Value.SetItemDynamicParameters(path, value);
    }

    protected override void InvokeDefaultAction(string path)
    {
        _provider.Value.InvokeDefaultAction(path);
    }

    protected override object? InvokeDefaultActionDynamicParameters(string path)
    {
        return _provider.Value.InvokeDefaultActionDynamicParameters(path);
    }

    protected override PSDriveInfo NewDrive(PSDriveInfo drive)
    {
        return _provider.Value.NewDrive(drive);
    }

    protected override object? NewDriveDynamicParameters()
    {
        return _provider.Value.NewDriveDynamicParameters();
    }

    protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
    {
        return _provider.Value.RemoveDrive(drive);
    }

    protected override Collection<PSDriveInfo> InitializeDefaultDrives()
    {
        return _provider.Value.InitializeDefaultDrives();
    }

    #region Content

    public void ClearContent(string path)
    {
        _provider.Value.ClearContent(path);
    }

    public object? ClearContentDynamicParameters(string path)
    {
        return _provider.Value.ClearContentDynamicParameters(path);
    }

    public IContentReader GetContentReader(string path)
    {
        return _provider.Value.GetContentReader(path);
    }

    public object? GetContentReaderDynamicParameters(string path)
    {
        return _provider.Value.GetContentReaderDynamicParameters(path);
    }

    public IContentWriter GetContentWriter(string path)
    {
        return _provider.Value.GetContentWriter(path);
    }

    public object? GetContentWriterDynamicParameters(string path)
    {
        return _provider.Value.GetContentWriterDynamicParameters(path);
    }

    #endregion

    #region Properties

    public void ClearProperty(string path, Collection<string> propertyToClear)
    {
        _provider.Value.ClearProperty(path, propertyToClear);
    }

    public object? ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear)
    {
        return _provider.Value.ClearPropertyDynamicParameters(path, propertyToClear);
    }

    public void GetProperty(string path, Collection<string> providerSpecificPickList)
    {
        _provider.Value.GetProperty(path, providerSpecificPickList);
    }

    public object? GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList)
    {
        return _provider.Value.GetPropertyDynamicParameters(path, providerSpecificPickList);
    }

    public void SetProperty(string path, PSObject propertyValue)
    {
        _provider.Value.SetProperty(path, propertyValue);
    }

    public object? SetPropertyDynamicParameters(string path, PSObject propertyValue)
    {
        return _provider.Value.SetPropertyDynamicParameters(path, propertyValue);
    }

    #endregion
}
