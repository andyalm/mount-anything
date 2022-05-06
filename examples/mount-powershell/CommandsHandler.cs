using MountAnything;

namespace MountPowershell;

public class CommandsHandler : PathHandler
{
    public static IItem CreateItem(ItemPath parentPath)
    {
        return new GenericContainerItem(parentPath, "commands");
    }
    
    public CommandsHandler(ItemPath path, IPathHandlerContext context) : base(path, context)
    {
    }

    protected override IItem? GetItemImpl()
    {
        return CreateItem(ParentPath);
    }

    protected override IEnumerable<IItem> GetChildItemsImpl()
    {
        return Context.InvokeCommand.InvokeScript("Get-Command")
            .Select(c => new CommandItem(Path, c));
    }
}