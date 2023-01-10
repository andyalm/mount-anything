using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;
using System.Runtime.Loader;
using MountAnything.Hosting.Abstractions;

namespace MountAnything.Hosting.Templates;

[CmdletProvider("MyProviderName", ProviderCapabilities.Filter | ProviderCapabilities.ExpandWildcards | ProviderCapabilities.Credentials)]
public class Provider : NavigationCmdletProvider,
    IContentCmdletProvider,
    IDynamicPropertyCmdletProvider,
    IProviderHost
{
    private static readonly object _providerMutex = new();
    private static IProviderImpl? _providerImpl;
    private static AssemblyLoadContext? _implAssemblyLoadContext;

    private IProviderImpl LoadProviderImplIsolatedContext()
    {
        _implAssemblyLoadContext = CreateAssemblyLoadContext();
        var implAssembly = _implAssemblyLoadContext.LoadFromAssemblyName(new AssemblyName("MyImplAssemblyName"));
        var frameworkAssembly = _implAssemblyLoadContext.LoadFromAssemblyName(new AssemblyName("MountAnything"));
        var providerImplType = frameworkAssembly
            .GetExportedTypes()
            .Single(t => typeof(IProviderImpl).IsAssignableFrom(t));

        try
        {
            return (IProviderImpl)Activator.CreateInstance(providerImplType, implAssembly)!;
        }
        catch (TargetInvocationException ex)
        {
            throw ex.InnerException!;
        }
    }

    private static AssemblyLoadContext CreateAssemblyLoadContext()
    {
        var modulePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var apiAssemblyDir = Path.Combine(modulePath, "Impl");
        return new ProviderAssemblyContext(apiAssemblyDir);
    }

    private IProviderImpl ProviderImpl
    {
        get
        {
            if (_providerImpl == null)
            {
                lock (_providerMutex)
                {
                    if (_providerImpl == null)
                    {
                        _providerImpl = LoadProviderImplIsolatedContext();
                    }
                }
            }

            ProviderHostAccessor.Current = this;

            return _providerImpl;
        }
    }
    
    protected override bool IsValidPath(string path) => ProviderImpl.ItemExists(path);
    protected override bool ItemExists(string path)
    {
        return ProviderImpl.ItemExists(path);
    }

    protected override object? ItemExistsDynamicParameters(string path)
    {
        return ProviderImpl.ItemExistsDynamicParameters(path);
    }

    protected override void GetItem(string path)
    {
        ProviderImpl.GetItem(path);
    }

    protected override object? GetItemDynamicParameters(string path)
    {
        return ProviderImpl.GetItemDynamicParameters(path);
    }
    
    protected override void GetChildItems(string path, bool recurse, uint depth)
    {
        ProviderImpl.GetChildItems(path, recurse, depth);
    }
    

    protected override object? GetChildItemsDynamicParameters(string path, bool recurse)
    {
        return ProviderImpl.GetChildItemsDynamicParameters(path, recurse);
    }
    
    protected override bool HasChildItems(string path)
    {
        return ProviderImpl.HasChildItems(path);
    }

    protected override bool IsItemContainer(string path)
    {
        return ProviderImpl.IsItemContainer(path);
    }

    protected override void NewItem(string path, string itemTypeName, object? newItemValue)
    {
        ProviderImpl.NewItem(path, itemTypeName, newItemValue);
    }

    protected override object? NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
    {
        return ProviderImpl.NewItemDynamicParameters(path, itemTypeName, newItemValue);
    }

    protected override void RemoveItem(string path, bool recurse)
    {
        ProviderImpl.RemoveItem(path, recurse);
    }

    protected override object? RemoveItemDynamicParameters(string path, bool recurse)
    {
        return ProviderImpl.RemoveItemDynamicParameters(path, recurse);
    }

    protected override void GetChildNames(string path, ReturnContainers returnContainers)
    {
        ProviderImpl.GetChildNames(path, returnContainers);
    }

    void IProviderHost.GetChildNamesDefaultImpl(string path, ReturnContainers returnContainers)
    {
        base.GetChildNames(path, returnContainers);
    }

    protected override object? GetChildNamesDynamicParameters(string path)
    {
        return ProviderImpl.GetChildNamesDynamicParameters(path);
    }

    protected override string[] ExpandPath(string path)
    {
        return ProviderImpl.ExpandPath(path);
    }

    protected override bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter)
    {
        return ProviderImpl.ConvertPath(path, filter, ref updatedPath, ref updatedFilter);
    }
    
    bool IProviderHost.ConvertPathDefaultImpl(string path, string filter, ref string updatedPath, ref string updatedFilter)
    {
        return base.ConvertPath(path, filter, ref updatedPath, ref updatedFilter);
    }

    protected override string NormalizeRelativePath(string path, string basePath)
    {
        return ProviderImpl.NormalizeRelativePath(path, basePath);
    }
    
    string IProviderHost.NormalizeRelativePathDefaultImpl(string path, string basePath)
    {
        return base.NormalizeRelativePath(path, basePath);
    }

    protected override ProviderInfo Start(ProviderInfo providerInfo)
    {
        return ProviderImpl.Start(providerInfo);
    }

    protected override object? StartDynamicParameters()
    {
        return ProviderImpl.StartDynamicParameters();
    }

    protected override void Stop()
    {
        ProviderImpl.Stop();
    }

    protected override void CopyItem(string path, string copyPath, bool recurse)
    {
        ProviderImpl.CopyItem(path, copyPath, recurse);
    }

    protected override object? CopyItemDynamicParameters(string path, string destination, bool recurse)
    {
        return ProviderImpl.CopyItemDynamicParameters(path, destination, recurse);
    }

    protected override void MoveItem(string path, string destination)
    {
        ProviderImpl.MoveItem(path, destination);
    }

    protected override object? MoveItemDynamicParameters(string path, string destination)
    {
        return ProviderImpl.MoveItemDynamicParameters(path, destination);
    }

    protected override void RenameItem(string path, string newName)
    {
        ProviderImpl.RenameItem(path, newName);
    }

    protected override object? RenameItemDynamicParameters(string path, string newName)
    {
        return ProviderImpl.RenameItemDynamicParameters(path, newName);
    }

    protected override void ClearItem(string path)
    {
        ProviderImpl.ClearItem(path);
    }

    protected override object? ClearItemDynamicParameters(string path)
    {
        return ProviderImpl.ClearItemDynamicParameters(path);
    }

    protected override void SetItem(string path, object value)
    {
        ProviderImpl.SetItem(path, value);
    }

    protected override object? SetItemDynamicParameters(string path, object value)
    {
        return ProviderImpl.SetItemDynamicParameters(path, value);
    }

    protected override void InvokeDefaultAction(string path)
    {
        ProviderImpl.InvokeDefaultAction(path);
    }

    protected override object? InvokeDefaultActionDynamicParameters(string path)
    {
        return ProviderImpl.InvokeDefaultActionDynamicParameters(path);
    }

    protected override PSDriveInfo NewDrive(PSDriveInfo drive)
    {
        return ProviderImpl.NewDrive(drive);
    }

    protected override object? NewDriveDynamicParameters()
    {
        return ProviderImpl.NewDriveDynamicParameters();
    }

    protected override PSDriveInfo RemoveDrive(PSDriveInfo drive)
    {
        return ProviderImpl.RemoveDrive(drive);
    }

    protected override Collection<PSDriveInfo> InitializeDefaultDrives()
    {
        return ProviderImpl.InitializeDefaultDrives();
    }

    #region Content

    public void ClearContent(string path)
    {
        ProviderImpl.ClearContent(path);
    }

    public object? ClearContentDynamicParameters(string path)
    {
        return ProviderImpl.ClearContentDynamicParameters(path);
    }

    public IContentReader GetContentReader(string path)
    {
        return ProviderImpl.GetContentReader(path);
    }

    public object? GetContentReaderDynamicParameters(string path)
    {
        return ProviderImpl.GetContentReaderDynamicParameters(path);
    }

    public IContentWriter GetContentWriter(string path)
    {
        return ProviderImpl.GetContentWriter(path);
    }

    public object? GetContentWriterDynamicParameters(string path)
    {
        return ProviderImpl.GetContentWriterDynamicParameters(path);
    }

    #endregion

    #region Properties

    public void ClearProperty(string path, Collection<string> propertyToClear)
    {
        ProviderImpl.ClearProperty(path, propertyToClear);
    }

    public object? ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear)
    {
        return ProviderImpl.ClearPropertyDynamicParameters(path, propertyToClear);
    }

    public void GetProperty(string path, Collection<string> providerSpecificPickList)
    {
        ProviderImpl.GetProperty(path, providerSpecificPickList);
    }

    public object? GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList)
    {
        return ProviderImpl.GetPropertyDynamicParameters(path, providerSpecificPickList);
    }

    public void SetProperty(string path, PSObject propertyValue)
    {
        ProviderImpl.SetProperty(path, propertyValue);
    }

    public object? SetPropertyDynamicParameters(string path, PSObject propertyValue)
    {
        return ProviderImpl.SetPropertyDynamicParameters(path, propertyValue);
    }

    #endregion

    object? IProviderHost.DynamicParameters => DynamicParameters;
    bool IProviderHost.Force => Force.IsPresent;
    char IProviderHost.ItemSeparator => ItemSeparator;
    PSDriveInfo IProviderHost.PSDriveInfo => PSDriveInfo;
    ProviderInfo IProviderHost.ProviderInfo => ProviderInfo;
    public void CopyProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty)
    {
        ProviderImpl.CopyProperty(sourcePath, sourceProperty, destinationPath, destinationProperty);
    }

    public object? CopyPropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath,
        string destinationProperty)
    {
        return ProviderImpl.CopyPropertyDynamicParameters(sourcePath, sourceProperty, destinationPath, destinationProperty);
    }

    public void MoveProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty)
    {
        ProviderImpl.MoveProperty(sourcePath, sourceProperty, destinationPath, destinationProperty);
    }

    public object? MovePropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath,
        string destinationProperty)
    {
        return ProviderImpl.MovePropertyDynamicParameters(sourcePath, sourceProperty, destinationPath, destinationProperty);
    }

    public void NewProperty(string path, string propertyName, string propertyTypeName, object value)
    {
        ProviderImpl.NewProperty(path, propertyName, propertyTypeName, value);
    }

    public object? NewPropertyDynamicParameters(string path, string propertyName, string propertyTypeName, object value)
    {
        return ProviderImpl.NewPropertyDynamicParameters(path, propertyName, propertyTypeName, value);
    }

    public void RemoveProperty(string path, string propertyName)
    {
        ProviderImpl.RemoveProperty(path, propertyName);
    }

    public object? RemovePropertyDynamicParameters(string path, string propertyName)
    {
        return ProviderImpl.RemovePropertyDynamicParameters(path, propertyName);
    }

    public void RenameProperty(string path, string sourceProperty, string destinationProperty)
    {
        ProviderImpl.RenameProperty(path, sourceProperty, destinationProperty);
    }

    public object? RenamePropertyDynamicParameters(string path, string sourceProperty, string destinationProperty)
    {
        return ProviderImpl.RenamePropertyDynamicParameters(path, sourceProperty, destinationProperty);
    }
}
