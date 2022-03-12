using System.Management.Automation;

namespace MountAnything.Hosting.Abstractions;

public interface IProviderHost
{
    object? DynamicParameters { get; }
    bool Force { get; }
    string? Filter { get; }
    char ItemSeparator { get; }
    PSDriveInfo PSDriveInfo { get; }
    
    CommandInvocationIntrinsics InvokeCommand { get; }
    
    void WriteError(ErrorRecord error);
    void WriteWarning(string message);
    void WriteDebug(string message);
    
    void WriteItemObject(object item, string path, bool isContainer);
    void WritePropertyObject(object propertyValue, string path);

    bool ConvertPathDefaultImpl(string path, string filter, ref string updatedPath, ref string updatedFilter);
    void GetChildNamesDefaultImpl(string path, ReturnContainers returnContainers);
}