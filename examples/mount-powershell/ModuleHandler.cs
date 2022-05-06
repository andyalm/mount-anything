using MountAnything;

namespace MountPowershell;

public class ModuleHandler : PathHandler
{
    public ModuleHandler(ItemPath path, IPathHandlerContext context) : base(path, context)
    {
    }

    protected override IItem? GetItemImpl()
    {
        var module = Context.InvokeCommand.InvokeScript($"Get-Module -Name {ItemName} -ErrorAction SilentlyContinue")
            .SingleOrDefault();
        if (module != null)
        {
            return new ModuleItem(ParentPath, module);
        }

        return null;
    }

    protected override IEnumerable<IItem> GetChildItemsImpl()
    {
        return Context.InvokeCommand.InvokeScript($"Get-Command -Module {ItemName}")
            .Select(c => new CommandItem(Path, c));
    }
}