using System.Reflection;
using System.Runtime.Loader;

namespace MountAnything.Hosting.Templates;

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

        // we don't want to load the Hosting.Abstractions assembly in this isolated AssemblyLoadContext, we want it to use the version from the global context
        if (!File.Exists(assemblyPath) || assemblyName.Name == "MountAnything.Hosting.Abstractions")
        {
            return null;
        }
        return LoadFromAssemblyPath(assemblyPath);
    }
}