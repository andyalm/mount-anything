using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.ResolveAnything;
using Microsoft.Extensions.DependencyInjection;

namespace MountAnything.Routing;

public class Router : IRoutable
{
    private readonly List<Route> _routes = new();
    private readonly Type _rootHandlerType;
    private readonly Lazy<IContainer> _rootContainer;
    private Action<ContainerBuilder> _containerRegistrations = _ => {};
    private Action<IServiceCollection> _serviceRegistrations = _ => {};

    public static Router Create<T>() where T : IPathHandler
    {
        return new Router(typeof(T));
    }

    private Router(Type rootHandlerType)
    {
        _rootHandlerType = rootHandlerType;
        _rootContainer = new Lazy<IContainer>(CreateRootContainer);
    }

    public void MapRegex<T>(string pattern, Action<Route>? createChildRoutes = null) where T : IPathHandler
    {
        var route = new Route(pattern, typeof(T));
        createChildRoutes?.Invoke(route);
        _routes.Add(route);
    }

    public void ConfigureServices(Action<IServiceCollection> serviceRegistration)
    {
        _serviceRegistrations += serviceRegistration;
    }

    public void ConfigureContainer(Action<ContainerBuilder> serviceRegistration)
    {
        _containerRegistrations += serviceRegistration;
    }

    public (IPathHandler Handler, ILifetimeScope Container) RouteToHandler(ItemPath path, IPathHandlerContext context)
    {
        var resolver = GetResolver(path);
        var lifetimeScope = _rootContainer.Value.BeginLifetimeScope(builder =>
        {
            resolver.RegisterServices(builder);
            builder.RegisterInstance(path);
            builder.RegisterInstance(context);
        });
        var handler = (IPathHandler)lifetimeScope.Resolve(resolver.HandlerType);

        return (handler, lifetimeScope);
    }
    
    public HandlerResolver GetResolver(ItemPath path)
    {
        if (path.IsRoot)
        {
            return new HandlerResolver(_rootHandlerType, _serviceRegistrations, _containerRegistrations);
        }
        
        foreach (var route in _routes)
        {
            if (route.TryGetResolver(path, out var match))
            {
                return match;
            }
        }

        throw new RoutingException($"No route matches path '{path}'");
    }
    
    private IContainer CreateRootContainer()
    {
        var builder = new ContainerBuilder();
        builder.RegisterInstance(this);
        builder.RegisterGeneric(typeof(ItemAncestorResolver<>)).As(typeof(IItemAncestor<>)).InstancePerLifetimeScope();
        builder.RegisterServices(_serviceRegistrations);
        _containerRegistrations.Invoke(builder);
        
        builder.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());

        return builder.Build();
    }
}