using System.Management.Automation;
using MountAnything;

namespace MountPowershell;

public class RootHandler : PathHandler
{
    public RootHandler(ItemPath path, IPathHandlerContext context) : base(path, context)
    {
    }

    protected override IItem? GetItemImpl()
    {
        return new RootItem();
    }

    protected override IEnumerable<IItem> GetChildItemsImpl()
    {
        yield return new GenericContainerItem(Path,"commands");
        yield return new GenericContainerItem(Path,"modules");
    }
}

public class RootItem : IItem
{
    public ItemPath FullPath => ItemPath.Root;
    public bool IsContainer => true;
    public PSObject ToPipelineObject(Func<ItemPath, string> pathResolver)
    {
        return new PSObject();
    }
}