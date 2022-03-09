using System.Reflection;
using System.Runtime.Loader;

namespace MountAnything.Build;

internal class ProviderAssemblyContext : AssemblyLoadContext
{
    public string DependencyPath { get; }

    public ProviderAssemblyContext(string dependencyDirPath)
    {
        DependencyPath = dependencyDirPath;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        string assemblyPath = Path.Combine(DependencyPath, $"{assemblyName.Name}.dll");

        if (!File.Exists(assemblyPath))
        {
            return null;
        }
        
        return LoadFromAssemblyPath(assemblyPath);
    }
}