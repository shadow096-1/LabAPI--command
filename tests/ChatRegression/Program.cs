using CommandSystem;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
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
var unsafeArgs = new ArraySegment<string>(new[] { "<size=999>hello</size>\n\u001b[31m" });
Check(commands[0].Execute(unsafeArgs, sender, out string response) && response.Contains("4 秒"), "Public response uses configured duration");
Check(Player.ReadyList.Where(p => p.IsReady).All(p => p.Hints.Count == 1 && p.Hints[0].Duration == 4), "Public delivery uses configured duration");
Check(Player.ReadyList.All(p => p.Broadcasts.Count == 0), "Public chat has no large broadcast");
Check(sender.Hints.Single().Text.StartsWith("<align=left><voffset=500><size=28>"), "Small chat text is left aligned and moved up");
Check(notReady.Broadcasts.Count == 0 && notReady.Hints.Count == 0, "Unready players receive nothing");
Check(!Logger.Messages.Single().Any(char.IsControl), "Log message and sender cannot inject control characters");
Check(sender.ConsoleMessages.Single().Contains("‹size=999›") && !sender.ConsoleMessages.Single().Contains("<color="), "Console copies strip framework markup but preserve safe user text");

ResetMessages();
plugin.Config.FactionBroadcastDuration = 3;
Check(commands[1].Execute(inputArgs, sender, out _), "Faction command succeeds");
Check(sender.Hints.Count == 1 && ally.Hints.Count == 1 && enemy.Hints.Count == 0 && admin.Hints.Count == 0, "Faction messages stay in sender faction");
Check(ally.Hints[0].Duration == 3, "Faction duration is configurable");
Check(Player.ReadyList.All(p => p.Broadcasts.Count == 0), "Faction chat has no large broadcast");

ResetMessages();
Check(plugin.Config.AdminReportBroadcastDuration == 10, "Reports default to ten seconds");
Check(commands[2].Execute(inputArgs, sender, out response) && response.Contains("1 名"), "Reports count ready admins only");
Check(admin.Broadcasts.Count == 1 && sender.Broadcasts.Count == 0 && ally.Broadcasts.Count == 0 && enemy.Broadcasts.Count == 0, "Reports reach only admins");
Check(admin.Broadcasts[0].Duration == 10 && admin.Hints.Single().Duration == 10, "Both report displays last ten seconds");
Check(admin.Hints.Single().Text.Contains(admin.Broadcasts.Single().Text), "Report retains large text and upper-left copy");
Check(Player.ReadyList.Where(p => p != admin).All(p => p.Hints.Count == 0), "Report hints are admin-only");
ResetMessages();
admin.RemoteAdminAccess = false;
Check(commands[2].Execute(inputArgs, sender, out response) && response.Contains("没有在线管理") && Logger.Messages.Count == 1, "Reports are logged when no admin is online");

ResetMessages();
plugin.Config.SendConsoleCopy = false;
commands[0].Execute(inputArgs, sender, out _);
Check(Player.ReadyList.All(p => p.ConsoleMessages.Count == 0), "Console copies can be disabled");
ResetMessages();
plugin.Config.HintVerticalOffset = 600;
plugin.Config.HintFontSize = 24;
commands[0].Execute(inputArgs, sender, out _);
Check(sender.Hints.Single().Text.StartsWith("<align=left><voffset=600><size=24>"), "Hint placement can be tuned for client display");
ResetMessages();
plugin.Disable();
foreach (var command in commands)
    Check(!command.Execute(inputArgs, sender, out _), command.Command + " rejects after disable");
Check(Player.ReadyList.All(p => p.Broadcasts.Count == 0 && p.Hints.Count == 0) && Logger.Messages.Count == 0, "Disabled commands produce no output");
Console.WriteLine($"All {checks} regression checks passed (test doubles; no game runtime).");

sealed class ConsoleSender : ICommandSender { }
