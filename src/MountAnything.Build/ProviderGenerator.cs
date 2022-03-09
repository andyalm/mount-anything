using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MountAnything.Build;

[Generator]
public class ProviderGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a factory that can create our custom syntax receiver
        //context.RegisterForSyntaxNotifications(() => new ProviderReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var providerName = context.GetMSBuildProperty("PowershellProviderName");
        if (providerName == null)
        {
            throw new InvalidOperationException(
                "The msbuild property 'PowershellProviderName' must be set in your project");
        }

        var rootNamespace = context.GetMSBuildProperty("RootNamespace");
        if (rootNamespace == null)
        {
            throw new InvalidOperationException("The msbuild property 'RootNamespace' must be set in your project");
        }
        
        var providerSource = GetSource("Provider.template.cs")
            .Replace("namespace MountAnything.Build;", $"namespace {rootNamespace};")
            .Replace("MyProviderName", providerName)
            .Replace("internal abstract class Provider", $"public class Provider");
            //.Replace("protected abstract Router CreateRouter();", "");
        context.AddSource("Provider.cs", providerSource);

        var assemblyContextSource = GetSource("ProviderAssemblyContext.template.cs")
            .Replace("namespace MountAnything.Build;", $"namespace {rootNamespace};");
        context.AddSource("ProviderAssemblyContext.cs", assemblyContextSource);
    }

    private string GetSource(string filename)
    {
        var fullyQualifiedResourceName = $"MountAnything.Build.{filename}";
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