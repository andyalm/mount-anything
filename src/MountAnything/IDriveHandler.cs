using System.Management.Automation;

namespace MountAnything;

public interface IDriveHandler
{
    PSDriveInfo NewDrive(PSDriveInfo driveInfo);
}

public interface INewDriveParameters<in T> where T : new()
{
    T NewDriveParameters { set; }
}

public class DefaultDriveHandler : IDriveHandler
{
    public PSDriveInfo NewDrive(PSDriveInfo driveInfo) => driveInfo;
}