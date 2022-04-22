using System.Management.Automation;
using MountAnything;

namespace MountPowershell;

public class GenericContainerItem : Item
{
    public GenericContainerItem(ItemPath parentPath, string name) : base(parentPath, new PSObject())
    {
        ItemName = name;
    }

    public override string ItemName { get; }
    public override bool IsContainer => true;
}