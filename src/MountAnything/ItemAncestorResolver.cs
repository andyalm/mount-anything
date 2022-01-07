using Autofac;
using MountAnything.Routing;

namespace MountAnything;

internal class ItemAncestorResolver<TItem> : IItemAncestor<TItem> where TItem : class, IItem
{
    private readonly Router _router;
    private readonly ILifetimeScope _lifetimeScope;
    private readonly ItemPath _currentPath;
    private readonly Lazy<TItem> _item;

    public ItemAncestorResolver(Router router, ILifetimeScope lifetimeScope, ItemPath currentPath)
    {
        _router = router;
        _lifetimeScope = lifetimeScope;
        _currentPath = currentPath;
        _item = new Lazy<TItem>(ResolveItem);
    }

    public TItem Item => _item.Value;

    private TItem ResolveItem()
    {
        var thisItemPath = _currentPath.Parent;
        do
        {
            var thisHandlerType = _router.GetResolver(thisItemPath).HandlerType;

            var handler = (IPathHandler)_lifetimeScope.Resolve(thisHandlerType, new TypedParameter(typeof(ItemPath), thisItemPath));
            if (handler.GetItem() is TItem item)
            {
                return item;
            }

            thisItemPath = thisItemPath.Parent;
        } while (!thisItemPath.IsRoot);

        throw new ItemUnresolvableException(
            $"Unable to find any parent paths of {_currentPath} that resolve to an item of type {typeof(TItem).FullName}");
    }
}