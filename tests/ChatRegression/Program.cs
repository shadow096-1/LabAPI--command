using CommandSystem;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Events.Handlers;
using PlayerRoles;
using SLTranslateCN;
using SLTranslateCN.Commands;

int checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    Console.WriteLine($"PASS: {name}");
    checks++;
}
void ResetMessages()
{
    Logger.Messages.Clear();
    foreach (Player p in Player.ReadyList)
    {
        p.Broadcasts.Clear();
        p.Hints.Clear();
        p.ConsoleMessages.Clear();
    }
}

ICommand[] commands = { new PublicChatCommand(), new FactionChatCommand(), new AdminReportCommand() };
var sender = new Player { Faction = Faction.FoundationStaff, Role = RoleTypeId.Scientist };
var ally = new Player { Faction = Faction.FoundationStaff };
var enemy = new Player { Faction = Faction.FoundationEnemy };
var admin = new Player { Faction = Faction.SCP, RemoteAdminAccess = true };
var notReady = new Player { IsReady = false, RemoteAdminAccess = true };
Player.ReadyList.AddRange(new[] { sender, ally, enemy, admin, notReady });
var inputArgs = new ArraySegment<string>(new[] { "hello" });
Check(!ChatService.IsEnabled, "No plugin instance is disabled");
foreach (var command in commands)
    Check(!command.Execute(inputArgs, sender, out _), command.Command + " rejects before enable");

var plugin = new Plugin();
plugin.Enable();
Check(!ChatService.IsEnabled, "Missing configuration is disabled");
plugin.Config = new ChatConfig();
Check(ChatService.IsEnabled, "Loaded enabled configuration allows commands");
plugin.Config.IsEnabled = false;
foreach (var command in commands)
    Check(!command.Execute(inputArgs, sender, out _), command.Command + " respects disabled config");
plugin.Config.IsEnabled = true;
sender.IsMuted = true;
Check(!commands[0].Execute(inputArgs, sender, out var mutedResponse) && mutedResponse.Contains("禁言"), "Muted player cannot use public chat");
Check(!commands[1].Execute(inputArgs, sender, out mutedResponse) && mutedResponse.Contains("禁言"), "Muted player cannot use faction chat");
Check(commands[2].Execute(inputArgs, sender, out _) && admin.Broadcasts.Count == 1, "Muted player can still report to admins");
sender.IsMuted = false;
ResetMessages();
foreach (var command in commands)
{
    Check(!command.Execute(inputArgs, new ConsoleSender(), out _), command.Command + " rejects non-player");
    Check(!command.Execute(default, sender, out _), command.Command + " rejects missing text");
    Check(!command.Execute(new ArraySegment<string>(new[] { "\r\n\t\u001b\u2028" }), sender, out _), command.Command + " rejects control-only text");
}
Check(ChatService.JoinArguments(new ArraySegment<string>(new[] { "skip", "hello", "world", "skip" }, 1, 2)) == "hello world", "Argument slice is respected");
Check(ChatService.Sanitize(" <b>x</b>\r\n\t\u001b\u2028 ") == "‹b›x‹/b›", "Markup and controls are sanitized");
Check(ChatService.JoinArguments(default) == "", "Default arguments are safe");

plugin.Config.PublicBroadcastDuration = 4;
sender.Nickname = "name\n[FakeLog]";
sender.DisplayName = "[Lv.99]<color=#fff>NotTheNickname</color>";
var unsafeArgs = new ArraySegment<string>(new[] { "<size=999>hello</size>\n\u001b[31m" });
Check(commands[0].Execute(unsafeArgs, sender, out string response) && response == "发送成功。", "Public response is status only");
Check(Player.ReadyList.Where(p => p.IsReady).All(p => p.Hints.Count == 1 && p.Hints[0].Duration >= 10), "Public delivery lasts at least ten seconds");
Check(Player.ReadyList.All(p => p.Broadcasts.Count == 0), "Public chat has no large broadcast");
Check(sender.Hints.Single().Text.StartsWith("<align=left><voffset=500><size=14>"), "Small chat text is left aligned and uses a smaller font");
Check(!sender.Hints.Single().Text.Contains("NotTheNickname") && !sender.Hints.Single().Text.Contains("[Lv.99]"), "Decorated display names never leak into chat");
Check(!ChatService.FormatSender(sender).Contains("科学家") && !ChatService.FormatSender(sender).Contains("基金会") &&
    sender.Hints.Single().Text.Contains("[公屏]name"), "Chat line contains only channel, nickname and message");
Check(notReady.Broadcasts.Count == 0 && notReady.Hints.Count == 0, "Unready players receive nothing");
Check(!Logger.Messages.Single().Any(char.IsControl), "Log message and sender cannot inject control characters");
Check(sender.ConsoleMessages.Single().Contains("‹size=999›") && !sender.ConsoleMessages.Single().Contains("<color="), "Console copies strip framework markup but preserve safe user text");
sender.Nickname = "很长很长很长很长很长很长很长的玩家名字";
ResetMessages();
commands[0].Execute(new ArraySegment<string>(new[] { new string('文', 200) }), sender, out _);
Check(sender.Hints.Single().Text.Contains("…") && !sender.Hints.Single().Text.Contains('\n') &&
    !sender.Hints.Single().Text.Contains(new string('文', 100)), "Long nickname and text fit one line");
Check(ChatService.FormatSender(sender).StartsWith("很长很长很长…"), "Nickname uses the first six characters and ellipsis");
sender.Nickname = "name";

ResetMessages();
plugin.Config.FactionBroadcastDuration = 3;
Check(commands[1].Execute(inputArgs, sender, out response) && response == "发送成功。", "Faction response is status only");
Check(sender.Hints.Count == 1 && ally.Hints.Count == 1 && enemy.Hints.Count == 0 && admin.Hints.Count == 0, "Faction messages stay in sender faction");
Check(ally.Hints[0].Duration >= 10, "Faction chat lasts at least ten seconds");
Check(Player.ReadyList.All(p => p.Broadcasts.Count == 0), "Faction chat has no large broadcast");

ResetMessages();
Check(plugin.Config.AdminReportBroadcastDuration == 10, "Reports default to ten seconds");
Check(commands[2].Execute(inputArgs, sender, out response) && response == "发送成功。", "Report response is status only");
Check(admin.Broadcasts.Count == 1 && sender.Broadcasts.Count == 0 && ally.Broadcasts.Count == 0 && enemy.Broadcasts.Count == 0, "Reports reach only admins");
Check(admin.Broadcasts[0].Duration == 10 && admin.Hints.Single().Duration == 10, "Both report displays last ten seconds");
Check(admin.Hints.Single().Text.Contains(admin.Broadcasts.Single().Text), "Report retains large text and upper-left copy");
Check(Player.ReadyList.Where(p => p != admin).All(p => p.Hints.Count == 0), "Report hints are admin-only");
ResetMessages();
admin.RemoteAdminAccess = false;
Check(!commands[2].Execute(inputArgs, sender, out response) && response == "发送失败。" && Logger.Messages.Count == 1, "Reports are logged when no admin is online");

ResetMessages();
plugin.Config.QqReportEnabled = true;
plugin.Config.QqBotApiUrl = "http://127.0.0.1:3000";
plugin.Config.QqBotAccessToken = "test-secret";
plugin.Config.QqReportGroupId = 123456789;
plugin.Config.QqReportAtQq = 987654321;
var forwarded = new List<(Uri Endpoint, string Token, string Body)>();
ReportForwarder.SendRequestAsync = (endpoint, token, body) =>
{
    forwarded.Add((endpoint, token, body));
    return Task.FromResult(true);
};
Check(commands[2].Execute(new ArraySegment<string>(new[] { "举报 \"引号\" \\ [CQ:at,qq=all]" }), sender, out response)
    && response == "发送成功。", "Offline report is accepted by QQ bot");
Check(forwarded.Count == 1 && forwarded[0].Endpoint.AbsoluteUri == "http://127.0.0.1:3000/send_group_msg"
    && forwarded[0].Token == "test-secret", "Bot request uses configured API and token");
using (var payload = System.Text.Json.JsonDocument.Parse(forwarded.Single().Body))
{
    var root = payload.RootElement;
    Check(root.GetProperty("group_id").GetInt64() == 123456789 &&
          root.GetProperty("message")[0].GetProperty("type").GetString() == "at" &&
          root.GetProperty("message")[0].GetProperty("data").GetProperty("qq").GetString() == "987654321", "Bot sends group message with explicit mention");
    Check(root.GetProperty("message")[1].GetProperty("data").GetProperty("text").GetString()!.Contains("[CQ:at,qq=all]"),
        "User text stays inside plain text segment");
}
admin.RemoteAdminAccess = true;
commands[2].Execute(inputArgs, sender, out response);
Check(forwarded.Count == 1 && admin.Broadcasts.Count == 1, "Online admin prevents bot forwarding");
admin.RemoteAdminAccess = false;
plugin.Config.QqBotApiUrl = "file:///tmp/untrusted";
Check(!commands[2].Execute(inputArgs, sender, out response) && response == "发送失败。" && forwarded.Count == 1,
    "Invalid API URL is rejected without a request");
plugin.Config.QqBotApiUrl = "http://127.0.0.1:3000";
enemy.DisplayName = "被举报玩家";
PlayerEvents.RaiseReportedPlayer(sender, enemy, "违规原因");
Check(forwarded.Count == 2 && forwarded.Last().Body.Contains("被举报玩家") && forwarded.Last().Body.Contains("违规原因"),
    "Player-list report forwards target and reason when no admin is online");
PlayerEvents.RaiseReportedCheater(sender, enemy, "作弊原因");
Check(forwarded.Count == 3 && forwarded.Last().Body.Contains("作弊举报") && forwarded.Last().Body.Contains("作弊原因"),
    "Player-list cheat report reaches QQ bot");
admin.RemoteAdminAccess = true;
PlayerEvents.RaiseReportedPlayer(sender, enemy, "管理员在线");
Check(forwarded.Count == 3, "Player-list reports are not forwarded when admin is online");
admin.RemoteAdminAccess = false;
plugin.Config.QqReportEnabled = false;
PlayerEvents.RaiseReportedPlayer(sender, enemy, "转发关闭");
Check(forwarded.Count == 3, "Native report hook respects disabled forwarding");
ResetMessages();

ResetMessages();
plugin.Config.SendConsoleCopy = false;
commands[0].Execute(inputArgs, sender, out _);
Check(Player.ReadyList.All(p => p.ConsoleMessages.Count == 0), "Console copies can be disabled");
ResetMessages();
plugin.Config.HintVerticalOffset = 600;
plugin.Config.HintFontSize = 24;
commands[0].Execute(inputArgs, sender, out _);
Check(sender.Hints.Single().Text.StartsWith("<align=left><voffset=600><size=16>"), "Hint placement is configurable and oversized legacy font settings are clamped");
ResetMessages();
plugin.Disable();
PlayerEvents.RaiseReportedPlayer(sender, enemy, "插件停用");
Check(forwarded.Count == 3, "Native report hooks are removed on disable");
foreach (var command in commands)
    Check(!command.Execute(inputArgs, sender, out _), command.Command + " rejects after disable");
Check(Player.ReadyList.All(p => p.Broadcasts.Count == 0 && p.Hints.Count == 0) && Logger.Messages.Count == 0, "Disabled commands produce no output");
Console.WriteLine($"All {checks} regression checks passed (test doubles; no game runtime).");

sealed class ConsoleSender : ICommandSender { }
