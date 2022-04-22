using System.Management.Automation;
using MountAnything;

namespace MountPowershell;

public class ModuleItem : Item
{
    public ModuleItem(ItemPath parentPath, PSObject underlyingObject) : base(parentPath, underlyingObject)
    {
        ItemName = underlyingObject.Property<string>("Name")!;
    }

    public override string ItemName { get; }
    public override bool IsContainer => true;
}