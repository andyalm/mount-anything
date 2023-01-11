using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace MountAnything.Routing;

public record HandlerResolver(Type HandlerType, Action<IServiceCollection> ServiceRegistrations,
    Action<ContainerBuilder> ContainerRegistrations)
{
    public void RegisterServices(ContainerBuilder builder)
    {
        builder.RegisterServices(ServiceRegistrations);
        ContainerRegistrations.Invoke(builder);
    }
}