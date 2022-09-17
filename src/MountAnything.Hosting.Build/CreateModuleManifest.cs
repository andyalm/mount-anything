using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using System.Text;

namespace MountAnything.Hosting.Build;

public class CreateModuleManifest : Microsoft.Build.Utilities.Task
{
    [Required]
    public ITaskItem Path { get; set; } = null!;

    public ITaskItem? RootModule { get; set; }

    [Required]
    public string ModuleVersion { get; set; } = null!;
    
    public string? Author { get; set; }
    
    public string? Copyright { get; set; }
    
    public string? Description { get; set; }

    public string? ReleaseNotes { get; set; }
    
    public string? LicenseUri { get; set; }
    
    public string? IconUri { get; set; }
    
    public string? ProjectUri { get; set; }
    
    public string? PowershellVersion { get; set; }

    public ITaskItem[] FormatsToProcess { get; set; } = Array.Empty<ITaskItem>();
    
    public ITaskItem[] NestedModules { get; set; } = Array.Empty<ITaskItem>();
    
    public string[] RequiredModules { get; set; } = Array.Empty<string>();
    
    public string[] FunctionsToExport { get; set; } = Array.Empty<string>();
    public string[] VariablesToExport { get; set; } = Array.Empty<string>();
    public string[] CmdletsToExport { get; set; } = Array.Empty<string>();
    public string[] AliasesToExport { get; set; } = Array.Empty<string>();

    public ITaskItem? WorkingDirectory { get; set; }

    public override bool Execute()
    {
        try
        {
            var command = new StringBuilder($"New-ModuleManifest -Path \"{Path.ItemSpec}\"");
            command.AddParameter(nameof(RootModule), RootModule);
            command.AddParameter(nameof(ModuleVersion), ModuleVersion);
            command.AddParameter(nameof(PowershellVersion), PowershellVersion);
            command.AddParameter(nameof(Author), Author);
            command.AddParameter(nameof(Copyright), Copyright);
            command.AddParameter(nameof(Description), Description);
            command.AddParameter(nameof(ReleaseNotes), ReleaseNotes);
            command.AddParameter(nameof(IconUri), IconUri);
            command.AddParameter(nameof(LicenseUri), LicenseUri);
            command.AddParameter(nameof(ProjectUri), ProjectUri);
            command.AddParameter(nameof(FormatsToProcess), FormatsToProcess);
            command.AddParameter(nameof(NestedModules), NestedModules);
            command.AddParameter(nameof(RequiredModules), RequiredModules);
            command.AddParameter(nameof(FunctionsToExport), FunctionsToExport);
            command.AddParameter(nameof(VariablesToExport), VariablesToExport);
            command.AddParameter(nameof(CmdletsToExport), CmdletsToExport);
            command.AddParameter(nameof(AliasesToExport), AliasesToExport);

            this.ExecTask(() => new Exec
            {
                Command = $"pwsh -Command \"{command.Replace("\"", "\\\"")}\"",
                WorkingDirectory = WorkingDirectory.FullPath()
            });

            return true;
        }
        catch (Exception e)
        {
            Log.LogError(e.ToString());
            return false;
        }
    }
}