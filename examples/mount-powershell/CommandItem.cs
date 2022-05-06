using System.Management.Automation;
using MountAnything;

namespace MountPowershell;

public class CommandItem : Item
{
    public CommandItem(ItemPath parentPath, PSObject underlyingObject) : base(parentPath, underlyingObject)
    {
        ItemName = Property<string>("Name")!;
    }

    public override string ItemName { get; }
    public override bool IsContainer => false;
}