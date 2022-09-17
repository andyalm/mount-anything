using System.Management.Automation;

namespace MountAnything;

public record DefaultDrive(string Name)
{
    public string? Description { get; init; }
    
    public PSCredential? Credential { get; init; }
}