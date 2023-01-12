using System.Management.Automation;
using MountAnything.Routing;

namespace MountAnything;

/// <summary>
/// A convenience base class implementing <see cref="IMountAnythingProvider"/> for when you want to use
/// dynamic drive parameters.
/// </summary>
/// <typeparam name="TDriveParameters">A class containing the dynamic parameters supported by the PSDriveInfo of this provider.</typeparam>
public abstract class MountAnythingProvider<TDriveParameters> : IMountAnythingProvider
    where TDriveParameters : class, new()
{
    PSDriveInfo IMountAnythingProvider.NewDrive(PSDriveInfo driveInfo, object? dynamicParameters)
    {
        return NewDrive(driveInfo, (TDriveParameters)dynamicParameters!);
    }
    
    object IMountAnythingProvider.CreateNewDriveDynamicParameters()
    {
        return new TDriveParameters();
    }
    
    /// <summary>
    /// When overridden by the subcless, returns <see cref="PSDriveInfo"/> for every PSDrive that should be created by default
    /// when the module for this provider is imported.
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable<PSDriveInfo> GetDefaultDrives(ProviderInfo providerInfo)
    {
        yield break;
    }

    /// <summary>
    /// When implemented by the subclass, creates a special <see cref="PSDriveInfo"/> based on the dynamic parameters specified via <see cref="TDriveParameters"/>.
    /// </summary>
    /// <param name="driveInfo">The base <see cref="PSDriveInfo"/> provided by the powershell engine. This will typically be passed into the base
    /// constructor of your PSDriveInfo subclass.</param>
    /// <param name="dynamicParameters">An object containing properties with <see cref="ParameterAttribute"/>'s for every dynamic property supported by this provider.</param>
    /// <returns></returns>
    protected abstract PSDriveInfo NewDrive(PSDriveInfo driveInfo, TDriveParameters dynamicParameters);

    public abstract Router CreateRouter();
}