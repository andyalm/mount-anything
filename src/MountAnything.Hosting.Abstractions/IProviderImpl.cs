using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Runtime.Loader;

namespace MountAnything.Hosting.Abstractions;

/// <summary>
/// The interface by which the MountAnything Powershell Provider instance can communicate
/// with the implementation hosted in a separate <see cref="AssemblyLoadContext"/>. The members of
/// this interface are based on the overridable members of <see cref="NavigationCmdletProvider"/>. However, since many
/// of those members are protected, we cannot simply re-use that class as our interface.
/// </summary>
public interface IProviderImpl
{
    #region CmdletProvider

    /// <summary>
    /// Gives the provider the opportunity to initialize itself.
    /// </summary>
    /// <param name="providerInfo">
    /// The information about the provider that is being started.
    /// </param>
    /// <remarks>
    /// The default implementation returns the ProviderInfo instance that
    /// was passed.
    ///
    /// To have session state maintain persisted data on behalf of the provider,
    /// the provider should derive from <see cref="System.Management.Automation.ProviderInfo"/>
    /// and add any properties or
    /// methods for the data it wishes to persist.  When Start gets called the
    /// provider should construct an instance of its derived ProviderInfo using the
    /// providerInfo that is passed in and return that new instance.
    /// </remarks>
    ProviderInfo Start(ProviderInfo providerInfo);

    /// <summary>
    /// Gets an object that defines the additional parameters for the Start implementation
    /// for a provider.
    /// </summary>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? StartDynamicParameters();

    /// <summary>
    /// Called by session state when the provider is being removed.
    /// </summary>
    /// <remarks>
    /// A provider should override this method to free up any resources that the provider
    /// was using.
    ///
    /// The default implementation does nothing.
    /// </remarks>
    void Stop();

        #endregion

    #region DriveCmdletProvider

    /// <summary>
    /// Gives the provider an opportunity to validate the drive
    /// that is being added. It also allows the provider to modify parts
    /// of the PSDriveInfo object. This may be done for performance or
    /// reliability reasons or to provide extra data to all calls using
    /// the Drive.
    /// </summary>
    /// <param name="drive">
    /// The proposed new drive.
    /// </param>
    /// <returns>
    /// The new drive that is to be added to the MSH namespace. This
    /// can either be the same <paramref name="drive"/> object that
    /// was passed in or a modified version of it.
    ///
    /// The default implementation returns the drive that was passed.
    /// </returns>
    /// <remarks>
    /// This method gives the provider an opportunity to associate
    /// provider specific data with a drive. This is done by deriving
    /// a new class from <see cref="System.Management.Automation.PSDriveInfo"/>
    /// and adding any properties, methods, or fields that are necessary.
    /// When this method gets called, the override should create an instance
    /// of the derived PSDriveInfo using the passed in PSDriveInfo. The derived
    /// PSDriveInfo should then be returned. Each subsequent call into the provider
    /// that uses this drive will have access to the derived PSDriveInfo via the
    /// PSDriveInfo property provided by the base class.
    ///
    /// Any failures should be sent to the <see cref="System.Management.Automation.Provider.CmdletProvider.WriteError(ErrorRecord)"/>
    /// method and null should be returned.
    /// </remarks>
    PSDriveInfo NewDrive(PSDriveInfo drive);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the New-PSDrive cmdlet.
    /// </summary>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? NewDriveDynamicParameters();

    /// <summary>
    /// Gives the provider an opportunity to clean up any provider specific data
    /// for the drive that is going to be removed.
    /// </summary>
    /// <param name="drive">
    /// The Drive object the represents the mounted drive.
    /// </param>
    /// <returns>
    /// If the drive can be removed it should return the drive that was passed
    /// in. If the drive cannot be removed, null should be returned or an exception
    /// should be thrown.
    ///
    /// The default implementation returns the drive that was passed.
    /// </returns>
    /// <remarks>
    /// A provider should override this method to free any resources that may be associated with
    /// the drive being removed.
    /// </remarks>
    PSDriveInfo RemoveDrive(PSDriveInfo drive);

    /// <summary>
    /// Gives the provider the ability to map drives after initialization.
    /// </summary>
    /// <returns>
    /// A collection of the drives the provider wants to be added to the session upon initialization.
    ///
    /// The default implementation returns an empty <see cref="System.Management.Automation.PSDriveInfo"/> collection.
    /// </returns>
    /// <remarks>
    /// After the Start method is called on a provider, the InitializeDefaultDrives
    /// method is called. This is an opportunity for the provider to
    /// mount drives that are important to it. For instance, the Active Directory
    /// provider might mount a drive for the defaultNamingContext if the
    /// machine is joined to a domain.
    ///
    /// All providers should mount a root drive to help the user with discoverability.
    /// This root drive might contain a listing of a set of locations that would be
    /// interesting as roots for other mounted drives. For instance, the Active
    /// Directory provider my create a drive that lists the naming contexts found
    /// in the namingContext attributes on the RootDSE. This will help users
    /// discover interesting mount points for other drives.
    /// </remarks>
    Collection<PSDriveInfo> InitializeDefaultDrives();

    #endregion

    #region ItemCmdletProvider

    /// <summary>
    /// Gets the item at the specified path.
    /// </summary>
    /// <param name="path">
    /// The path to the item to retrieve.
    /// </param>
    /// <returns>
    /// Nothing is returned, but all objects should be written to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user access to the provider objects using
    /// the get-item and get-childitem cmdlets.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    ///
    /// By default overrides of this method should not write objects that are generally hidden from
    /// the user unless the Force property is set to true. For instance, the FileSystem provider should
    /// not call WriteItemObject for hidden or system files unless the Force property is set to true.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    void GetItem(string path);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the get-item cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? GetItemDynamicParameters(string path);

    /// <summary>
    /// Sets the item specified by the path.
    /// </summary>
    /// <param name="path">
    /// The path to the item to set.
    /// </param>
    /// <param name="value">
    /// The value of the item specified by the path.
    /// </param>
    /// <returns>
    /// Nothing.  The item that was set should be passed to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to modify provider objects using
    /// the set-item cmdlet.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    ///
    /// By default overrides of this method should not set or write objects that are generally hidden from
    /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
    /// the path represents an item that is hidden from the user and Force is set to false.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    void SetItem(string path, object value);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the set-item cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="value">
    /// The value of the item specified by the path.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? SetItemDynamicParameters(string path, object value);

    /// <summary>
    /// Clears the item specified by the path.
    /// </summary>
    /// <param name="path">
    /// The path to the item to clear.
    /// </param>
    /// <returns>
    /// Nothing.  The item that was cleared should be passed to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to clear provider objects using
    /// the clear-item cmdlet.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    ///
    /// By default overrides of this method should not clear or write objects that are generally hidden from
    /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
    /// the path represents an item that is hidden from the user and Force is set to false.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    void ClearItem(string path);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the clear-item cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? ClearItemDynamicParameters(string path);

    /// <summary>
    /// Invokes the default action on the specified item.
    /// </summary>
    /// <param name="path">
    /// The path to the item to perform the default action on.
    /// </param>
    /// <returns>
    /// Nothing.  The item that was set should be passed to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// The default implementation does nothing.
    ///
    /// Providers override this method to give the user the ability to invoke provider objects using
    /// the invoke-item cmdlet. Think of the invocation as a double click in the Windows Shell. This
    /// method provides a default action based on the path that was passed.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    ///
    /// By default overrides of this method should not invoke objects that are generally hidden from
    /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
    /// the path represents an item that is hidden from the user and Force is set to false.
    /// </remarks>
    void InvokeDefaultAction(string path);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the invoke-item cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? InvokeDefaultActionDynamicParameters(string path);

    /// <summary>
    /// Determines if an item exists at the specified path.
    /// </summary>
    /// <param name="path">
    /// The path to the item to see if it exists.
    /// </param>
    /// <returns>
    /// True if the item exists, false otherwise.
    /// </returns>
    /// <returns>
    /// Nothing.  The item that was set should be passed to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to check for the existence of provider objects using
    /// the set-item cmdlet.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    ///
    /// The implementation of this method should take into account any form of access to the object that may
    /// make it visible to the user.  For instance, if a user has write access to a file in the file system
    /// provider bug not read access, the file still exists and the method should return true.  Sometimes this
    /// may require checking the parent to see if the child can be enumerated.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    bool ItemExists(string path);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the test-path cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? ItemExistsDynamicParameters(string path);

    /// <summary>
    /// Providers must override this method to verify the syntax and semantics
    /// of their paths.
    /// </summary>
    /// <param name="path">
    /// The path to check for validity.
    /// </param>
    /// <returns>
    /// True if the path is syntactically and semantically valid for the provider, or
    /// false otherwise.
    /// </returns>
    /// <remarks>
    /// This test should not verify the existence of the item at the path. It should
    /// only perform syntactic and semantic validation of the path.  For instance, for
    /// the file system provider, that path should be canonicalized, syntactically verified,
    /// and ensure that the path does not refer to a device.
    /// </remarks>
    bool IsValidPath(string path);

    /// <summary>
    /// Expand a provider path that contains wildcards to a list of provider
    /// paths that the path represents.Only called for providers that declare
    /// the ExpandWildcards capability.
    /// </summary>
    /// <param name="path">
    /// The path to expand. Expansion must be consistent with the wildcarding
    /// rules of PowerShell's WildcardPattern class.
    /// </param>
    /// <returns>
    /// A list of provider paths that this path expands to. They must all exist.
    /// </returns>
    string[] ExpandPath(string path);

    #endregion

    #region ContainerCmdletProvider

    /// <summary>
    /// Gets the children of the item at the specified path.
    /// </summary>
    /// <param name="path">
    /// The path (or name in a flat namespace) to the item from which to retrieve the children.
    /// </param>
    /// <param name="recurse">
    /// True if all children in a subtree should be retrieved, false if only a single
    /// level of children should be retrieved. This parameter should only be true for
    /// the NavigationCmdletProvider derived class.
    /// </param>
    /// <param name="depth">
    /// Limits the depth of recursion; uint.MaxValue performs full recursion.
    /// </param>
    /// <returns>
    /// Nothing is returned, but all objects should be written to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user access to the provider objects using
    /// the get-childitem cmdlets.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    ///
    /// By default overrides of this method should not write objects that are generally hidden from
    /// the user unless the Force property is set to true. For instance, the FileSystem provider should
    /// not call WriteItemObject for hidden or system files unless the Force property is set to true.
    ///
    /// The provider implementation is responsible for preventing infinite recursion when there are
    /// circular links and the like. An appropriate terminating exception should be thrown if this
    /// situation occurs.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    void GetChildItems(string path, bool recurse, uint depth);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the get-childitem cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="recurse">
    /// True if all children in a subtree should be retrieved, false if only a single
    /// level of children should be retrieved. This parameter should only be true for
    /// the NavigationCmdletProvider derived class.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? GetChildItemsDynamicParameters(string path, bool recurse);

    /// <summary>
    /// Gets names of the children of the specified path.
    /// </summary>
    /// <param name="path">
    /// The path to the item from which to retrieve the child names.
    /// </param>
    /// <param name="returnContainers">
    /// Determines if all containers should be returned or only those containers that match the
    /// filter(s).
    /// </param>
    /// <returns>
    /// Nothing is returned, but all objects should be written to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user access to the provider objects using
    /// the get-childitem  -name cmdlet.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class. The exception to this
    /// is if <paramref name="returnContainers"/> is true, then any child name for a container should
    /// be returned even if it doesn't match the Filter, Include, or Exclude.
    ///
    /// By default overrides of this method should not write the names of objects that are generally hidden from
    /// the user unless the Force property is set to true. For instance, the FileSystem provider should
    /// not call WriteItemObject for hidden or system files unless the Force property is set to true.
    ///
    /// The provider implementation is responsible for preventing infinite recursion when there are
    /// circular links and the like. An appropriate terminating exception should be thrown if this
    /// situation occurs.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    void GetChildNames(string path, ReturnContainers returnContainers);

    /// <summary>
    /// Gets a new provider-specific path and filter (if any) that corresponds to the given
    /// path.
    /// </summary>
    /// <param name="path">
    /// The path to the item. Unlike most other provider APIs, this path is likely to
    /// contain PowerShell wildcards.
    /// </param>
    /// <param name="filter">
    /// The provider-specific filter currently applied.
    /// </param>
    /// <param name="updatedPath">
    /// The new path to the item.
    /// </param>
    /// <param name="updatedFilter">
    /// The new filter.
    /// </param>
    /// <returns>
    /// True if the path or filter were altered. False otherwise.
    /// </returns>
    /// <remarks>
    /// Providers override this method if they support a native filtering syntax that
    /// can offer performance improvements over wildcard matching done by the PowerShell
    /// engine.
    /// If the provider can handle a portion (or all) of the PowerShell wildcard with
    /// semantics equivalent to the PowerShell wildcard, it may adjust the path to exclude
    /// the PowerShell wildcard.
    /// If the provider can augment the PowerShell wildcard with an approximate filter (but
    /// not replace it entirely,) it may simply return a filter without modifying the path.
    /// In this situation, PowerShell's wildcarding will still be applied to a smaller result
    /// set, resulting in improved performance.
    ///
    /// The default implementation of this method leaves both Path and Filter unmodified.
    ///
    /// PowerShell wildcarding semantics are handled by the System.Management.Automation.Wildcardpattern
    /// class.
    /// </remarks>
    bool ConvertPath(string path, string filter, ref string updatedPath, ref string updatedFilter);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the get-childitem -name cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? GetChildNamesDynamicParameters(string path);

    /// <summary>
    /// Renames the item at the specified path to the new name provided.
    /// </summary>
    /// <param name="path">
    /// The path to the item to rename.
    /// </param>
    /// <param name="newName">
    /// The name to which the item should be renamed. This name should always be
    /// relative to the parent container.
    /// </param>
    /// <returns>
    /// Nothing is returned, but the renamed items should be written to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to rename provider objects using
    /// the rename-item cmdlet.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    ///
    /// By default overrides of this method should not allow renaming objects that are generally hidden from
    /// the user unless the Force property is set to true. For instance, the FileSystem provider should
    /// not allow renaming of a hidden or system file unless the Force property is set to true.
    ///
    /// This method is intended for the modification of the item's name only and not for Move operations.
    /// An error should be written to <see cref="CmdletProvider.WriteError"/> if the <paramref name="newName"/>
    /// parameter contains path separators or would cause the item to change its parent location.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    void RenameItem(string path, string newName);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the rename-item cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="newName">
    /// The name to which the item should be renamed. This name should always be
    /// relative to the parent container.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? RenameItemDynamicParameters(string path, string newName);

    /// <summary>
    /// Creates a new item at the specified path.
    /// </summary>
    /// <param name="path">
    /// The path to the item to create.
    /// </param>
    /// <param name="itemTypeName">
    /// The provider defined type for the object to create.
    /// </param>
    /// <param name="newItemValue">
    /// This is a provider specific type that the provider can use to create a new
    /// instance of an item at the specified path.
    /// </param>
    /// <returns>
    /// Nothing is returned, but the renamed items should be written to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to create new provider objects using
    /// the new-item cmdlet.
    ///
    /// The <paramref name="itemTypeName"/> parameter is a provider specific string that the user specifies to tell
    /// the provider what type of object to create.  For instance, in the FileSystem provider the <c>type</c>
    /// parameter can take a value of "file" or "directory". The comparison of this string should be
    /// case-insensitive and you should also allow for least ambiguous matches. So if the provider allows
    /// for the types "file" and "directory", only the first letter is required to disambiguate.
    /// If <paramref name="itemTypeName"/> refers to a type the provider cannot create, the provider should produce
    /// an <see cref="ArgumentException"/> with a message indicating the types the provider can create.
    ///
    /// The <paramref name="newItemValue"/> parameter can be any type of object that the provider can use
    /// to create the item. It is recommended that the provider accept at a minimum strings, and an instance
    /// of the type of object that would be returned from GetItem() for this path. <see cref="LanguagePrimitives.ConvertTo(System.Object, System.Type)"/>
    /// can be used to convert some types to the desired type.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    void NewItem(string path, string itemTypeName, object? newItemValue);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the new-item cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="itemTypeName">
    /// The provider defined type of the item to create.
    /// </param>
    /// <param name="newItemValue">
    /// This is a provider specific type that the provider can use to create a new
    /// instance of an item at the specified path.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? NewItemDynamicParameters(string path, string itemTypeName, object newItemValue);

    /// <summary>
    /// Removes (deletes) the item at the specified path.
    /// </summary>
    /// <param name="path">
    /// The path to the item to remove.
    /// </param>
    /// <param name="recurse">
    /// True if all children in a subtree should be removed, false if only a single
    /// level of children should be removed. This parameter should only be true for
    /// NavigationCmdletProvider and its derived classes.
    /// </param>
    /// <returns>
    /// Nothing should be returned or written from this method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to allow the user the ability to remove provider objects using
    /// the remove-item cmdlet.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    ///
    /// By default overrides of this method should not remove objects that are generally hidden from
    /// the user unless the Force property is set to true. For instance, the FileSystem provider should
    /// not remove a hidden or system file unless the Force property is set to true.
    ///
    /// The provider implementation is responsible for preventing infinite recursion when there are
    /// circular links and the like. An appropriate terminating exception should be thrown if this
    /// situation occurs.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    void RemoveItem(string path, bool recurse);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the remove-item cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="recurse">
    /// True if all children in a subtree should be removed, false if only a single
    /// level of children should be removed. This parameter should only be true for
    /// NavigationCmdletProvider and its derived classes.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? RemoveItemDynamicParameters(string path, bool recurse);

    /// <summary>
    /// Determines if the item at the specified path has children.
    /// </summary>
    /// <param name="path">
    /// The path to the item to see if it has children.
    /// </param>
    /// <returns>
    /// True if the item has children, false otherwise.
    /// </returns>
    /// <returns>
    /// Nothing is returned, but all objects should be written to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the provider infrastructure the ability to determine
    /// if a particular provider object has children without having to retrieve all the child items.
    ///
    /// For implementers of <see cref="ContainerCmdletProvider"/> classes and those derived from it,
    /// if a null or empty path is passed,
    /// the provider should consider any items in the data store to be children
    /// and return true.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    bool HasChildItems(string path);

    /// <summary>
    /// Copies an item at the specified path to an item at the <paramref name="copyPath"/>.
    /// </summary>
    /// <param name="path">
    /// The path of the item to copy.
    /// </param>
    /// <param name="copyPath">
    /// The path of the item to copy to.
    /// </param>
    /// <param name="recurse">
    /// Tells the provider to recurse sub-containers when copying.
    /// </param>
    /// <returns>
    /// Nothing is returned, but all the objects that were copied should be written to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to copy provider objects using
    /// the copy-item cmdlet.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path and items being copied
    /// meets those requirements by accessing the appropriate property from the base class.
    ///
    /// By default overrides of this method should not copy objects over existing items unless the Force
    /// property is set to true. For instance, the FileSystem provider should not copy c:\temp\foo.txt over
    /// c:\bar.txt if c:\bar.txt already exists unless the Force parameter is true.
    ///
    /// If <paramref name="copyPath"/> exists and is a container then Force isn't required and <paramref name="path"/>
    /// should be copied into the <paramref name="copyPath"/> container as a child.
    ///
    /// If <paramref name="recurse"/> is true, the provider implementation is responsible for
    /// preventing infinite recursion when there are circular links and the like. An appropriate
    /// terminating exception should be thrown if this situation occurs.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    void CopyItem(string path, string copyPath, bool recurse);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the copy-item cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="destination">
    /// The path of the item to copy to.
    /// </param>
    /// <param name="recurse">
    /// Tells the provider to recurse sub-containers when copying.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? CopyItemDynamicParameters(string path, string destination, bool recurse);

    #endregion

    #region NavigationCmdletProvider

    /// <summary>
    /// Determines if the item specified by the path is a container.
    /// </summary>
    /// <param name="path">
    /// The path to the item to determine if it is a container.
    /// </param>
    /// <returns>
    /// true if the item specified by path is a container, false otherwise.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to check
    /// to see if a provider object is a container using the test-path -container cmdlet.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    bool IsItemContainer(string path);
    
    /// <summary>
    /// Normalizes the path that was passed in and returns the normalized path
    /// as a relative path to the basePath that was passed.
    /// </summary>
    /// <param name="path">
    /// A fully qualified provider specific path to an item. The item should exist
    /// or the provider should write out an error.
    /// </param>
    /// <param name="basePath">
    /// The path that the return value should be relative to.
    /// </param>
    /// <returns>
    /// A normalized path that is relative to the basePath that was passed. The
    /// provider should parse the path parameter, normalize the path, and then
    /// return the normalized path relative to the basePath.
    /// </returns>
    /// <remarks>
    /// This method does not have to be purely syntactical parsing of the path. It
    /// is encouraged that the provider actually use the path to lookup in its store
    /// and create a relative path that matches the casing, and standardized path syntax.
    /// 
    /// Note, the base class implementation uses GetParentPath, GetChildName, and MakePath
    /// to normalize the path and then make it relative to basePath. All string comparisons
    /// are done using StringComparison.InvariantCultureIgnoreCase.
    /// </remarks>
    string NormalizeRelativePath(string path, string basePath);

    /// <summary>
    /// Moves the item specified by path to the specified destination.
    /// </summary>
    /// <param name="path">
    /// The path to the item to be moved.
    /// </param>
    /// <param name="destination">
    /// The path of the destination container.
    /// </param>
    /// <returns>
    /// Nothing is returned, but all the objects that were moved should be written to the WriteItemObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to move provider objects using
    /// the move-item cmdlet.
    ///
    /// Providers that declare <see cref="System.Management.Automation.Provider.ProviderCapabilities"/>
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path and items being moved
    /// meets those requirements by accessing the appropriate property from the base class.
    ///
    /// By default overrides of this method should not move objects over existing items unless the Force
    /// property is set to true. For instance, the FileSystem provider should not move c:\temp\foo.txt over
    /// c:\bar.txt if c:\bar.txt already exists unless the Force parameter is true.
    ///
    /// If <paramref name="destination"/> exists and is a container then Force isn't required and <paramref name="path"/>
    /// should be moved into the <paramref name="destination"/> container as a child.
    ///
    /// The default implementation of this method throws an <see cref="System.Management.Automation.PSNotSupportedException"/>.
    /// </remarks>
    void MoveItem(string path, string destination);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to
    /// the move-item cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="destination">
    /// The path of the destination container.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
    ///
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? MoveItemDynamicParameters(string path, string destination);

    #endregion

    #region Content

    void ClearContent(string path);

    object? ClearContentDynamicParameters(string path);

    IContentReader GetContentReader(string path);

    object? GetContentReaderDynamicParameters(string path);
    IContentWriter GetContentWriter(string path);
    
    object? GetContentWriterDynamicParameters(string path);

    #endregion
    
    #region Properties

    void ClearProperty(string path, Collection<string> propertyToClear);

    object? ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear);

    void GetProperty(string path, Collection<string>? providerSpecificPickList);

    object? GetPropertyDynamicParameters(string path, Collection<string>? providerSpecificPickList);

    void SetProperty(string path, PSObject propertyValue);

    object? SetPropertyDynamicParameters(string path, PSObject propertyValue);
    
    /// <summary>
    /// Copies a property of the item at the specified path to a new property on the
    /// destination item.
    /// </summary>
    /// <param name="sourcePath">
    /// The path to the item on which to copy the property.
    /// </param>
    /// <param name="sourceProperty">The name of the property to copy.</param>
    /// <param name="destinationPath">
    /// The path to the item on which to copy the property to.
    /// </param>
    /// <param name="destinationProperty">
    /// The destination property to copy to.
    /// </param>
    /// <returns>
    /// Nothing.  The new property that was copied to should be passed to the WritePropertyObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to copy properties of provider objects
    /// using the copy-itemproperty cmdlet.
    /// 
    /// Providers that declare <see cref="T:System.Management.Automation.Provider.ProviderCapabilities" />
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    /// 
    /// By default overrides of this method should not copy properties from or to objects that are generally hidden from
    /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
    /// the path represents an item that is hidden from the user and Force is set to false.
    /// </remarks>
    void CopyProperty(
      string sourcePath,
      string sourceProperty,
      string destinationPath,
      string destinationProperty);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to the
    /// copy-itemproperty cmdlet.
    /// </summary>
    /// <param name="sourcePath">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="sourceProperty">The name of the property to copy.</param>
    /// <param name="destinationPath">
    /// The path to the item on which to copy the property to.
    /// </param>
    /// <param name="destinationProperty">
    /// The destination property to copy to.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="T:System.Management.Automation.RuntimeDefinedParameterDictionary" />.
    /// 
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? CopyPropertyDynamicParameters(
      string sourcePath,
      string sourceProperty,
      string destinationPath,
      string destinationProperty);

    /// <summary>Moves a property on an item specified by the path.</summary>
    /// <param name="sourcePath">
    /// The path to the item on which to move the property.
    /// </param>
    /// <param name="sourceProperty">The name of the property to move.</param>
    /// <param name="destinationPath">
    /// The path to the item on which to move the property to.
    /// </param>
    /// <param name="destinationProperty">
    /// The destination property to move to.
    /// </param>
    /// <returns>
    /// Nothing.  The new property that was created should be passed to the WritePropertyObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to move properties from one provider object
    /// to another using the move-itemproperty cmdlet.
    /// 
    /// Providers that declare <see cref="T:System.Management.Automation.Provider.ProviderCapabilities" />
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    /// 
    /// By default overrides of this method should not move properties on or to objects that are generally hidden from
    /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
    /// the path represents an item that is hidden from the user and Force is set to false.
    /// </remarks>
    void MoveProperty(
      string sourcePath,
      string sourceProperty,
      string destinationPath,
      string destinationProperty);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to the
    /// move-itemproperty cmdlet.
    /// </summary>
    /// <param name="sourcePath">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="sourceProperty">The name of the property to copy.</param>
    /// <param name="destinationPath">
    /// The path to the item on which to copy the property to.
    /// </param>
    /// <param name="destinationProperty">
    /// The destination property to copy to.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="T:System.Management.Automation.RuntimeDefinedParameterDictionary" />.
    /// 
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? MovePropertyDynamicParameters(
      string sourcePath,
      string sourceProperty,
      string destinationPath,
      string destinationProperty);

    /// <summary>Creates a new property on the specified item.</summary>
    /// <param name="path">
    /// The path to the item on which the new property should be created.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property that should be created.
    /// </param>
    /// <param name="propertyTypeName">
    /// The type of the property that should be created.
    /// </param>
    /// <param name="value">
    /// The new value of the property that should be created.
    /// </param>
    /// <returns>
    /// Nothing.  The new property that was created should be passed to the WritePropertyObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to add properties to provider objects
    /// using the new-itemproperty cmdlet.
    /// 
    /// Providers that declare <see cref="T:System.Management.Automation.Provider.ProviderCapabilities" />
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    /// 
    /// By default overrides of this method should not create new properties on objects that are generally hidden from
    /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
    /// the path represents an item that is hidden from the user and Force is set to false.
    /// </remarks>
    void NewProperty(string path, string propertyName, string propertyTypeName, object? value);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to the
    /// new-itemproperty cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property that should be created.
    /// </param>
    /// <param name="propertyTypeName">
    /// The type of the property that should be created.
    /// </param>
    /// <param name="value">
    /// The new value of the property that should be created.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="T:System.Management.Automation.RuntimeDefinedParameterDictionary" />.
    /// 
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? NewPropertyDynamicParameters(
      string path,
      string propertyName,
      string propertyTypeName,
      object? value);

    /// <summary>Removes a property on the item specified by the path.</summary>
    /// <param name="path">
    /// The path to the item on which the property should be removed.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property to be removed.
    /// </param>
    /// <returns>Nothing.</returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to remove properties from provider objects
    /// using the remove-itemproperty cmdlet.
    /// 
    /// Providers that declare <see cref="T:System.Management.Automation.Provider.ProviderCapabilities" />
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    /// 
    /// By default overrides of this method should not remove properties on objects that are generally hidden from
    /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
    /// the path represents an item that is hidden from the user and Force is set to false.
    /// </remarks>
    void RemoveProperty(string path, string propertyName);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to the
    /// remove-itemproperty cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="propertyName">
    /// The name of the property that should be removed.
    /// </param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="T:System.Management.Automation.RuntimeDefinedParameterDictionary" />.
    /// 
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? RemovePropertyDynamicParameters(string path, string propertyName);

    /// <summary>Renames a property of the item at the specified path.</summary>
    /// <param name="path">
    /// The path to the item on which to rename the property.
    /// </param>
    /// <param name="sourceProperty">The property to rename.</param>
    /// <param name="destinationProperty">The new name of the property.</param>
    /// <returns>
    /// Nothing.  The new property that was renamed should be passed to the WritePropertyObject method.
    /// </returns>
    /// <remarks>
    /// Providers override this method to give the user the ability to rename properties of provider objects
    /// using the rename-itemproperty cmdlet.
    /// 
    /// Providers that declare <see cref="T:System.Management.Automation.Provider.ProviderCapabilities" />
    /// of ExpandWildcards, Filter, Include, or Exclude should ensure that the path passed meets those
    /// requirements by accessing the appropriate property from the base class.
    /// 
    /// By default overrides of this method should not rename properties on objects that are generally hidden from
    /// the user unless the Force property is set to true. An error should be sent to the WriteError method if
    /// the path represents an item that is hidden from the user and Force is set to false.
    /// </remarks>
    void RenameProperty(string path, string sourceProperty, string destinationProperty);

    /// <summary>
    /// Gives the provider an opportunity to attach additional parameters to the
    /// rename-itemproperty cmdlet.
    /// </summary>
    /// <param name="path">
    /// If the path was specified on the command line, this is the path
    /// to the item to get the dynamic parameters for.
    /// </param>
    /// <param name="sourceProperty">The property to rename.</param>
    /// <param name="destinationProperty">The new name of the property.</param>
    /// <returns>
    /// Overrides of this method should return an object that has properties and fields decorated with
    /// parsing attributes similar to a cmdlet class or a
    /// <see cref="T:System.Management.Automation.RuntimeDefinedParameterDictionary" />.
    /// 
    /// The default implementation returns null. (no additional parameters)
    /// </returns>
    object? RenamePropertyDynamicParameters(
      string path,
      string sourceProperty,
      string destinationProperty);

    #endregion
}