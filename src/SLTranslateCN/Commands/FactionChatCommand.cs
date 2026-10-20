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

        if (!Player.TryGet(sender, out Player player))
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
        response = $"阵营消息已发送给 {recipients} 名同阵营玩家。";
        return true;
    }
}

