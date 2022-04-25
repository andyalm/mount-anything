using Microsoft.Build.Framework;

namespace MountAnything.Hosting.Build;

public class StageMountAnythingHostProject : Microsoft.Build.Utilities.Task
{
    [Required]
    public string? PowershellProviderName { get; set; }

    [Required]
    public string? ImplAssemblyName { get; set; }
    
    [Required]
    public string? RootNamespace { get; set; }
    
    [Required]
    public string? StagingDir { get; set; }
    
    public override bool Execute()
    {
        try
        {
            var providerSource = GetSource("Provider.cs")
                .Replace("namespace MountAnything.Hosting.Templates;", $"namespace {RootNamespace};")
                .Replace("MyProviderName", PowershellProviderName)
                .Replace("MyImplAssemblyName", ImplAssemblyName);
            Directory.CreateDirectory(StagingDir!);
            File.WriteAllText(Path.Combine(StagingDir!, "Provider.cs"), providerSource);

            var assemblyContextSource = GetSource("ProviderAssemblyContext.cs")
                .Replace("namespace MountAnything.Hosting.Templates;", $"namespace {RootNamespace};");
            File.WriteAllText(Path.Combine(StagingDir!, "ProviderAssemblyContext.cs"), assemblyContextSource);

            var projectSource = GetSource("Host.csproj");
            File.WriteAllText(Path.Combine(StagingDir!, $"{PowershellProviderName}.Host.csproj"), projectSource);

            return true;
        }
        catch (Exception e)
        {
            Log.LogError(e.ToString());
            return false;
        }
    }
    
    private string GetSource(string filename)
    {
        var fullyQualifiedResourceName = $"MountAnything.Hosting.Build.templates.{filename}";
        using var stream =
            typeof(StageMountAnythingHostProject).Assembly.GetManifestResourceStream(fullyQualifiedResourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"No embedded resource could be found at {fullyQualifiedResourceName}");
        }
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}