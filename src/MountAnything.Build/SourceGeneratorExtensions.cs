using Microsoft.CodeAnalysis;

namespace MountAnything.Build;

public static class SourceGeneratorExtensions
{
    public static string? GetMSBuildProperty(
        this GeneratorExecutionContext context,
        string name)
    {
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value);
        return value;
    }   
}