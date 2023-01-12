using System.Management.Automation;
using MountAnything.Routing;

namespace MountAnything;

public interface IMountAnythingProvider
{
    Router CreateRouter();
    IEnumerable<PSDriveInfo> GetDefaultDrives(ProviderInfo providerInfo) => Enumerable.Empty<PSDriveInfo>();
    PSDriveInfo NewDrive(PSDriveInfo driveInfo, object? dynamicParameters) => driveInfo;
    object? CreateNewDriveDynamicParameters() => null;
}