using System.Management.Automation;
using Microsoft.Extensions.DependencyInjection;

namespace MountAnything.Routing;

public static class ServiceCollectionExtensions
{
    public static void AddDriveInfo<TDriveInfo>(this IServiceCollection services) where TDriveInfo : PSDriveInfo
    {
        services.AddTransient<TDriveInfo>(s => (TDriveInfo)s.GetRequiredService<IPathHandlerContext>().DriveInfo);
    }
}