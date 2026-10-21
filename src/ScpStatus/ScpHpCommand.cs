using System;
using CommandSystem;

namespace ScpStatus;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class ScpHpCommand : ICommand
{
    public string Command => "scphp";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "设置 SCP 固定血量：scphp 173 3500；scphp 173 off；scphp list";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var plugin = ScpStatusPlugin.Instance;
        if (plugin == null)
        {
            response = "SCP 状态插件未启用。";
            return false;
        }

        if (arguments.Count == 1 && arguments.Array![arguments.Offset].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            response = plugin.ListHealth();
            return true;
        }

        if (arguments.Count != 2)
        {
            response = Description;
            return false;
        }

        var args = arguments.Array!;
        if (!ScpStatusPlugin.TryParseScp(args[arguments.Offset], out var role))
        {
            response = "角色格式错误，例如 173、096、049、049-2。";
            return false;
        }

        string value = args[arguments.Offset + 1];
        if (value.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            response = plugin.SetHealth(role, null);
            return true;
        }

        if (!float.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float hp) ||
            float.IsNaN(hp) || float.IsInfinity(hp) || hp < 1 || hp > 100000)
        {
            response = "血量应为 1 到 100000 的数字；off 取消覆盖。";
            return false;
        }

        response = plugin.SetHealth(role, hp);
        return true;
    }
}
