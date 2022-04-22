using MountAnything.Routing;

namespace MountAnything;

public interface IMountAnythingProvider
{
    Router CreateRouter();

    IEnumerable<DefaultDrive> GetDefaultDrives() => Enumerable.Empty<DefaultDrive>();
}