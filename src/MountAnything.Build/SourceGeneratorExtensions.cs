using Microsoft.CodeAnalysis;

namespace MountAnything.Build;

public static class SourceGeneratorExtensions
{
    public static string? GetMSBuildPropertyOrDefault(
        this GeneratorExecutionContext context,
        string name)
    {
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value))
        {
            return value;
        }

        return null;
    }
    
    public static string GetMSBuildProperty(
        this GeneratorExecutionContext context,
        string name)
    {
        return context.GetMSBuildPropertyOrDefault(name)
               ?? throw new InvalidOperationException($"The msbuild property '{name}' must be set in your project");
    }
}