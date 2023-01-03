using MountAnything.Routing;

namespace MountAnything;

public static class DynamicParametersExtensions
{
    public static object? CreateDynamicParameters(this HandlerResolver handlerResolver, Type handlerParameterInterface)
    {
        return CreateDynamicParameters(handlerResolver.HandlerType, handlerParameterInterface);
    }
    
    public static object? CreateDynamicParameters(this IDriveHandler driveHandler, Type handlerParameterInterface)
    {
        return CreateDynamicParameters(driveHandler.GetType(), handlerParameterInterface);
    }

    private static object? CreateDynamicParameters(Type handlerType, Type handlerParameterInterface)
    {
        var parameterInterface = handlerType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerParameterInterface);
        if (parameterInterface != null)
        {
            var parameterType = parameterInterface.GetGenericArguments().Single();
            return Activator.CreateInstance(parameterType);
        }

        return null;
    }

    public static void SetDynamicParameters(this IPathHandler handler, Type handlerParameterInterface, object? dynamicParameters)
    {
        SetDynamicParameters((object)handler, handlerParameterInterface, dynamicParameters);
    }
    
    public static void SetDynamicParameters(this IDriveHandler handler, Type handlerParameterInterface, object? dynamicParameters)
    {
        SetDynamicParameters((object)handler, handlerParameterInterface, dynamicParameters);
    }

    private static void SetDynamicParameters(object handlerInstance, Type handlerParameterInterface,
        object? dynamicParameters)
    {
        var parameterInterface = handlerInstance.GetType().GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerParameterInterface);
        if (parameterInterface != null && dynamicParameters != null)
        {
            var parameterProperty = parameterInterface.GetProperties()
                .Single(p => p.CanWrite && dynamicParameters.GetType().IsAssignableFrom(p.PropertyType));
            
            parameterProperty.SetValue(handlerInstance, dynamicParameters);
        }
    }
}