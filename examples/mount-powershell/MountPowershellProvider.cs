using MountAnything;
using MountAnything.Routing;

namespace MountPowershell;

public class MountPowershellProvider : IMountAnythingProvider
{
    public Router CreateRouter()
    {
        var router = Router.Create<RootHandler>();
        router.MapLiteral<ModulesHandler>("modules", modules =>
        {
            modules.Map<ModuleHandler>(module =>
            {
                module.Map<CommandHandler>();
            });
        });
        router.MapLiteral<CommandsHandler>("commands", commands =>
        {
            commands.Map<CommandHandler>();
        });
        return router;
    }

    public IEnumerable<DefaultDrive> GetDefaultDrives()
    {
        yield return new DefaultDrive("pwsh")
        {
            Description = "Navigate powershell objects as a hierarchical virtual drive"
        };
    }
}
