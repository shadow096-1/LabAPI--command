using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace SLTranslateCN.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class AdminReportCommand : ICommand
{
    public string Command => "ac";

    public string[] Aliases => new[] { "report", "adminchat" };

    public string Description => "举报并私信在线管理：.ac <举报内容>";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ChatService.IsEnabled)
        {
            response = "聊天插件当前未启用。";
            return false;
        }

        if (!Player.TryGet(sender, out Player player))
        {
            response = "只有玩家可以使用 .ac。";
            return false;
        }

        string text = ChatService.JoinArguments(arguments);
        if (string.IsNullOrWhiteSpace(text))
        {
            response = "用法：.ac <举报内容>";
            return false;
        }

        int admins = ChatService.SendAdminReport(player, text);
        response = admins > 0
            ? $"举报已私信给 {admins} 名在线管理。"
            : "举报已记录到服务器日志，但当前没有在线管理。";
        return true;
    }
}

