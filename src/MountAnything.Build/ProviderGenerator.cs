using Microsoft.CodeAnalysis;

namespace MountAnything.Build;

/// <summary>
/// A roslyn source generator that generates a powershell provider that loads a <c>MountAnything</c> <c>Router</c> and path handlers
/// in its own <c>AssemblyLoadContext</c>.
/// </summary>
[Generator]
public class ProviderGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) { }

    public void Execute(GeneratorExecutionContext context)
    {
        var providerName = context.GetMSBuildProperty("PowershellProviderName");
        var rootNamespace = context.GetMSBuildPropertyOrDefault("RootNamespace") ?? context.GetMSBuildProperty("ProjectName");
        var implAssemblyName = context.GetMSBuildProperty("ImplAssemblyName");

        var providerSource = GetSource("Provider.cs")
            .Replace("namespace MountAnything.Build;", $"namespace {rootNamespace};")
            .Replace("MyProviderName", providerName)
            .Replace("MyImplAssemblyName", implAssemblyName);
        context.AddSource("Provider.cs", providerSource);

        var assemblyContextSource = GetSource("ProviderAssemblyContext.cs")
            .Replace("namespace MountAnything.Build;", $"namespace {rootNamespace};");
        context.AddSource("ProviderAssemblyContext.cs", assemblyContextSource);
    }

    private string GetSource(string filename)
    {
        var fullyQualifiedResourceName = $"MountAnything.Build.templates.{filename}";
        using var stream =
            typeof(ProviderGenerator).Assembly.GetManifestResourceStream(fullyQualifiedResourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"No embedded resource could be found at {fullyQualifiedResourceName}");
        }
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}