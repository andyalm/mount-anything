using System.Reflection;
using Autofac;

namespace MountAnything.Routing;

internal class InjectedItemResolver
{
    private readonly Router _router;
    private readonly ILifetimeScope _lifetimeScope;

    public InjectedItemResolver(Router router, ILifetimeScope lifetimeScope)
    {
        _router = router;
        _lifetimeScope = lifetimeScope;
    }

    public object ResolveItem(ItemPath itemPath, Type itemType)
    {
        var thisItemPath = itemPath;
        do
        {
            var thisHandlerType = _router.GetResolver(thisItemPath).HandlerType;
            var thisItemType = thisHandlerType.GetCustomAttribute<InjectableItemAttribute>()?.ItemType;

            if (thisItemType == itemType)
            {
                var handler =
                    (IPathHandler)_lifetimeScope.Resolve(thisHandlerType, new NamedParameter("path", thisItemPath));
                var item = handler.GetItem();
                if (item == null)
                {
                    throw new RoutingException($"Item at {itemPath} does not exist");
                }

                return item;
            }

            thisItemPath = thisItemPath.Parent;
        } while (!thisItemPath.IsRoot);

        throw new RoutingException(
            $"Unable to find any parent paths of {itemPath} that resolve to an item of type {itemType}");
    }
}