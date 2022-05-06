using MountAnything;

namespace MountPowershell;

public class CommandHandler : PathHandler
{
    public CommandHandler(ItemPath path, IPathHandlerContext context) : base(path, context)
    {
    }

    protected override IItem? GetItemImpl()
    {
        var cmd = Context.InvokeCommand.InvokeScript($"Get-Command -Name {ItemName} -ErrorAction SilentlyContinue").SingleOrDefault();
        if (cmd != null)
        {
            return new CommandItem(ParentPath, cmd);
        }

        return null;
    }

    protected override IEnumerable<IItem> GetChildItemsImpl()
    {
        yield break;
    }
}