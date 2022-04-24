using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using System.IO;
using System.Text;

namespace MountAnything.Build;

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
            var command = new StringBuilder($"New-ModuleManifest -Path \"{Path.FullPath()}\"");
            command.AddParameter(nameof(RootModule), RootModule);
            command.AddParameter(nameof(ModuleVersion), ModuleVersion);
            command.AddParameter(nameof(PowershellVersion), PowershellVersion);

            this.ExecTask(() => new Exec
            {
                Command = $"pwsh -Command '{command}'",
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