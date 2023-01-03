using System.Text.RegularExpressions;
using Autofac;

namespace MountAnything.Routing;

public static class RoutingExtensions
{
    private const string ItemRegex = @"[a-z0-9-_\.:]+";
    
    public static void Map<T>(this IRoutable router, Action<Route>? createChildRoutes = null) where T : IPathHandler
    {
        router.MapRegex<T>(ItemRegex, createChildRoutes);
    }
    
    public static void Map<T>(this IRoutable router, string routeValueName, Action<Route>? createChildRoutes = null) where T : IPathHandler
    {
        router.MapRegex<T>($"(?<{routeValueName}>{ItemRegex})", createChildRoutes);
    }
    
    public static void Map<THandler, TTypedString>(this IRoutable router, Action<Route> createChildRoutes)
        where THandler : IPathHandler
        where TTypedString : TypedString
    {
        var routeValueName = typeof(TTypedString).Name;
        router.MapRegex<THandler>($"(?<{routeValueName}>{ItemRegex})", route =>
        {
            route.RegisterServices((match, builder) =>
            {
                builder.Register(_ =>
                    (TTypedString)Activator.CreateInstance(typeof(TTypedString), match.Values[routeValueName])!);
            });
            createChildRoutes.Invoke(route);
        });
    }

    public static void MapRecursive<THandler, TTypedPath>(this IRoutable router,
        Action<Route>? createChildRoutes = null)
        where THandler : IPathHandler
        where TTypedPath : TypedItemPath
    {
        var routeValueName = typeof(TTypedPath).Name;
        router.MapRegex<THandler>($"(?<{routeValueName}>.+)", route =>
        {
            route.RegisterServices((match, builder) =>
            {
                builder.Register(_ =>
                    (TTypedPath)Activator.CreateInstance(typeof(TTypedPath), new ItemPath(match.Values[routeValueName]))!);
            });
            createChildRoutes?.Invoke(route);
        });
    }
        

    public static void MapLiteral<T>(this IRoutable router, string literal, Action<Route>? createChildRoutes = null)
        where T : IPathHandler
    {
        router.MapRegex<T>(Regex.Escape(literal), createChildRoutes);
    }
}