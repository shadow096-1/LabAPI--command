using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace SLTranslateCN.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class FactionChatCommand : ICommand
{
    public string Command => "c";

    public string[] Aliases => new[] { "fc", "teamchat" };

    public string Description => "发送阵营频道消息：.c <内容>";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ChatService.IsEnabled)
        {
            response = "聊天插件当前未启用。";
            return false;
        }

        if (!Player.TryGet(sender, out Player? player) || player is null)
        {
            response = "只有玩家可以使用 .c。";
            return false;
        }

        string text = ChatService.JoinArguments(arguments);
        if (string.IsNullOrWhiteSpace(text))
        {
            response = "用法：.c <要发送到同阵营频道的内容>";
            return false;
        }

        int recipients = ChatService.SendFaction(player, text);
        response = recipients > 0 ? "发送成功。" : "发送失败。";
        return recipients > 0;
    }
}

