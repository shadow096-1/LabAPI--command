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

        if (!Player.TryGet(sender, out Player? player) || player is null)
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

        bool accepted = ChatService.SendAdminReport(player, text);
        response = accepted ? "发送成功。" : "发送失败。";
        return accepted;
    }
}

