using MountAnything;

namespace MountPowershell;

public class ModulesHandler : PathHandler
{
    public static IItem CreateItem(ItemPath parentPath)
    {
        return new GenericContainerItem(parentPath, "modules");
    }
    
    public ModulesHandler(ItemPath path, IPathHandlerContext context) : base(path, context)
    {
    }

    protected override IItem? GetItemImpl()
    {
        return CreateItem(ParentPath);
    }

    protected override IEnumerable<IItem> GetChildItemsImpl()
    {
        return Context.InvokeCommand.InvokeScript("Get-Module")
            .Select(m => new ModuleItem(Path, m));
    }
}