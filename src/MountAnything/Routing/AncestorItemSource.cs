using Autofac;
using Autofac.Builder;
using Autofac.Core;

namespace MountAnything.Routing;

/// <summary>
/// A custom Autofac <see cref="IRegistrationSource"/> that will resolve any service that implements <see cref="IItem"/>
/// by walking up the existing path hierarchy looking for a path that resolves to this type of item.
/// </summary>
internal class AncestorItemSource : IRegistrationSource
{
    public IEnumerable<IComponentRegistration> RegistrationsFor(Service service, Func<Service, IEnumerable<ServiceRegistration>> registrationAccessor)
    {
        if (service is not TypedService typedService)
        {
            yield break;
        }
        if (!typeof(IItem).IsAssignableFrom(typedService.ServiceType) || typedService.ServiceType.IsAbstract)
        {
            yield break;
        }

        var builder = RegistrationBuilder.ForDelegate(typedService.ServiceType, (context, _) =>
        {
            var itemPath = context.Resolve<ItemPath>();
            return context.Resolve<InjectedItemResolver>().ResolveItem(itemPath, typedService.ServiceType);
        });
        yield return builder.CreateRegistration();
    }

    public bool IsAdapterForIndividualComponents => false;
}