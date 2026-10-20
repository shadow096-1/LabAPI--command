using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace SLTranslateCN;

public static class ChatService
{
    private const string PublicPrefix = "<color=#00D5FF>[公屏]</color>";
    private const string FactionPrefix = "<color=#8BFF7A>[阵营]</color>";
    private const string AdminPrefix = "<color=#FF6969>[举报]</color>";

    public static bool IsEnabled => Plugin.Instance?.Config?.IsEnabled == true;

    public static string JoinArguments(ArraySegment<string> arguments)
    {
        if (arguments.Count == 0 || arguments.Array is null)
        {
            return string.Empty;
        }

        return Sanitize(string.Join(" ", arguments.Array.Skip(arguments.Offset).Take(arguments.Count)));
    }

    public static string FormatSender(Player player)
    {
        string faction = GetFactionName(player.Faction);
        string state = player.IsAlive ? "存活" : "死亡/观战";
        string role = GetRoleName(player.Role);
        string name = Sanitize(player.DisplayName ?? player.Nickname ?? "未知玩家");

        return $"{name} <color=#FFD36E>[{faction}｜{state}｜{role}]</color>";
    }

    public static string Sanitize(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            builder.Append(char.IsControl(character) || character == '\u2028' || character == '\u2029'
                ? ' '
                : character == '<' ? '‹' : character == '>' ? '›' : character);
        }

        return builder.ToString().Trim();
    }

    public static void SendPublic(Player sender, string text)
    {
        string message = $"{PublicPrefix} {FormatSender(sender)}：{Sanitize(text)}";
        ushort duration = Plugin.Instance?.Config?.PublicBroadcastDuration ?? 10;

        foreach (Player player in Player.ReadyList.Where(player => player.IsReady))
        {
            SendPlayerMessage(player, message, duration);
        }

        Logger.Info($"[PublicChat] {Sanitize(sender.LogName)}: {Sanitize(text)}");
    }

    public static int SendFaction(Player sender, string text)
    {
        string message = $"{FactionPrefix} {FormatSender(sender)}：{Sanitize(text)}";
        ushort duration = Plugin.Instance?.Config?.FactionBroadcastDuration ?? 10;
        Faction faction = sender.Faction;
        int recipients = 0;

        foreach (Player player in Player.ReadyList.Where(player => player.IsReady && player.Faction == faction))
        {
            SendPlayerMessage(player, message, duration);
            recipients++;
        }

        Logger.Info($"[FactionChat:{faction}] {Sanitize(sender.LogName)}: {Sanitize(text)}");
        return recipients;
    }

    public static int SendAdminReport(Player sender, string text)
    {
        string message = $"{AdminPrefix} {FormatSender(sender)}：{Sanitize(text)}";
        ushort duration = Plugin.Instance?.Config?.AdminReportBroadcastDuration ?? 10;
        List<Player> admins = Player.ReadyList
            .Where(player => player.IsReady && player.RemoteAdminAccess)
            .ToList();

        foreach (Player admin in admins)
        {
            SendPlayerMessage(admin, message, duration, "red", showBroadcast: true);
        }

        Logger.Warn($"[AdminReport] {Sanitize(sender.LogName)}: {Sanitize(text)}");
        return admins.Count;
    }

    public static string GetFactionName(Faction faction)
    {
        return faction switch
        {
            Faction.FoundationStaff => "基金会",
            Faction.FoundationEnemy => "混沌",
            Faction.SCP => "SCP",
            Faction.Unclassified => "未分类",
            _ => faction.ToString()
        };
    }

    public static string GetRoleName(RoleTypeId role)
    {
        return role switch
        {
            RoleTypeId.None => "未分配",
            RoleTypeId.Spectator => "观察者",
            RoleTypeId.ClassD => "D级人员",
            RoleTypeId.Scientist => "科学家",
            RoleTypeId.FacilityGuard => "设施警卫",
            RoleTypeId.NtfPrivate => "九尾狐列兵",
            RoleTypeId.NtfSergeant => "九尾狐中士",
            RoleTypeId.NtfSpecialist => "九尾狐专家",
            RoleTypeId.NtfCaptain => "九尾狐队长",
            RoleTypeId.ChaosConscript => "混沌征召兵",
            RoleTypeId.ChaosRifleman => "混沌步枪手",
            RoleTypeId.ChaosMarauder => "混沌掠夺者",
            RoleTypeId.ChaosRepressor => "混沌镇压者",
            RoleTypeId.Tutorial => "教程角色",
            RoleTypeId.Overwatch => "监管者",
            RoleTypeId.Filmmaker => "摄像师",
            _ when role.ToString().StartsWith("Scp", StringComparison.OrdinalIgnoreCase) => role.ToString().ToUpperInvariant(),
            _ => role.ToString()
        };
    }

    private static void SendPlayerMessage(Player player, string message, ushort duration, string consoleColor = "cyan", bool showBroadcast = false)
    {
        if (showBroadcast)
        {
            player.SendBroadcast(message, duration, shouldClearPrevious: false);
        }

        ChatConfig? config = Plugin.Instance?.Config;
        int offset = Math.Max(0, Math.Min(700, config?.HintVerticalOffset ?? 500));
        int size = Math.Max(16, Math.Min(40, config?.HintFontSize ?? 28));
        player.SendHint($"<align=left><voffset={offset}><size={size}>{message}</size></voffset></align>", duration);

        if (Plugin.Instance?.Config?.SendConsoleCopy ?? true)
        {
            player.SendConsoleMessage(StripRichText(message), consoleColor);
        }
    }

    private static string StripRichText(string value)
    {
        StringBuilder builder = new(value.Length);
        bool insideTag = false;

        foreach (char character in value)
        {
            if (character == '<')
            {
                insideTag = true;
                continue;
            }

            if (character == '>')
            {
                insideTag = false;
                continue;
            }

            if (!insideTag)
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}

