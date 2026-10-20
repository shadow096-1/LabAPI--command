using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace SLTranslateCN.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class PublicChatCommand : ICommand
{
    public string Command => "bc";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "发送公屏聊天：.bc <内容>";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ChatService.IsEnabled)
        {
            response = "聊天插件当前未启用。";
            return false;
        }

        if (!Player.TryGet(sender, out Player? player) || player is null)
        {
            response = "只有玩家可以使用 .bc。";
            return false;
        }

        string text = ChatService.JoinArguments(arguments);
        if (string.IsNullOrWhiteSpace(text))
        {
            response = "用法：.bc <要发送到公屏的内容>";
            return false;
        }

        ChatService.SendPublic(player, text);
        response = "发送成功。";
        return true;
    }
}

