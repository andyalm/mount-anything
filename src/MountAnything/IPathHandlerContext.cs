using System.Management.Automation;

namespace MountAnything;

public interface IPathHandlerContext
{
    ICache Cache { get; }
    void WriteDebug(string message);
    void WriteWarning(string message);
    bool Force { get; }
    CommandInvocationIntrinsics InvokeCommand { get; }
    PSCredential? Credential { get; }
}