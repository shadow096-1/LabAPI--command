using System;
using System.Text.RegularExpressions;
using CommandSystem;

namespace PlayerLevels;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class LevelBoostCommand : ICommand
{
    private static readonly Regex SteamId = new(@"^[0-9]{17}@steam$", RegexOptions.IgnoreCase);
    public string Command => "levelboost";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "管理员设置经验倍数：levelboost 7656119xxxxxxxxxx@steam 2；倍数 1 为取消";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var plugin = PlayerLevelsPlugin.Instance;
        if (plugin == null)
        {
            response = "等级插件未启用。";
            return false;
        }
        if (arguments.Count < 1 || arguments.Count > 2)
        {
            response = Description;
            return false;
        }
        var userId = arguments.Array![arguments.Offset];
        if (!SteamId.IsMatch(userId))
        {
            response = "需要完整的 17 位 Steam ID，格式：7656119xxxxxxxxxx@steam";
            return false;
        }
        if (arguments.Count == 1)
        {
            response = plugin.GetMultiplier(userId);
            return true;
        }
        if (!int.TryParse(arguments.Array![arguments.Offset + 1], out var multiplier))
        {
            response = "倍数必须是整数。";
            return false;
        }
        response = plugin.SetMultiplier(userId, multiplier);
        return response.Contains("已设为");
    }
}
