using System;
using LabApi.Features;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LabPlugin = LabApi.Loader.Features.Plugins.Plugin<SLTranslateCN.ChatConfig>;

namespace SLTranslateCN;

public sealed class Plugin : LabPlugin
{
    public static Plugin? Instance { get; private set; }

    public override string Name => "SLTranslateCN";

    public override string Description => "Adds Chinese client-console chat commands for public, faction, and admin-report messages.";

    public override string Author => "OpenAI";

    public override Version Version { get; } = new(1, 3, 0);

    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);

    public override string ConfigFileName { get; set; } = "sl_translate_cn.yml";

    public override void Enable()
    {
        Instance = this;
        PlayerEvents.ReportedPlayer += OnReportedPlayer;
        PlayerEvents.ReportedCheater += OnReportedCheater;
    }

    public override void Disable()
    {
        PlayerEvents.ReportedPlayer -= OnReportedPlayer;
        PlayerEvents.ReportedCheater -= OnReportedCheater;
        Instance = null;
    }

    private static void OnReportedPlayer(PlayerReportedPlayerEventArgs ev)
    {
        ForwardNativeReport(ev.Player, ev.Target, ev.Reason, "玩家举报");
    }

    private static void OnReportedCheater(PlayerReportedCheaterEventArgs ev)
    {
        ForwardNativeReport(ev.Player, ev.Target, ev.Reason, "作弊举报");
    }

    private static void ForwardNativeReport(Player reporter, Player target, string reason, string kind)
    {
        if (!ChatService.IsEnabled ||
            System.Linq.Enumerable.Any(Player.ReadyList, player => player.IsReady && player.RemoteAdminAccess))
        {
            return;
        }

        string targetName = ChatService.Sanitize(target?.DisplayName ?? target?.Nickname ?? "未知玩家");
        string content = $"{kind}：{targetName}；原因：{reason}";
        ReportForwarder.TryForward(Instance?.Config, reporter, content);
    }
}

