using System;
using CommandSystem;

namespace PlayerLevels;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class LevelReloadCommand : ICommand
{
    public string Command => "levelreload";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "手工修改 players.txt 或 multipliers.txt 后，重新载入两份记录";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count != 0)
        {
            response = Description;
            return false;
        }
        var plugin = PlayerLevelsPlugin.Instance;
        if (plugin == null)
        {
            response = "等级插件未启用。";
            return false;
        }
        response = plugin.ReloadRecords();
        return response.StartsWith("已重新载入", StringComparison.Ordinal);
    }
}
