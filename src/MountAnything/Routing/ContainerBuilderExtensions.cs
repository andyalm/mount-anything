using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace MountAnything.Routing;

public static class ContainerBuilderExtensions
{
    public static void RegisterServices(this ContainerBuilder builder, Action<IServiceCollection> registerServices)
    {
        var services = new ServiceCollection();
        registerServices.Invoke(services);
        builder.Populate(services);
    }
}