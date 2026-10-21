// Test doubles only: these do not validate binary compatibility with the game.
namespace CommandSystem
{
    public interface ICommandSender { }
    public interface ICommand
    {
        string Command { get; }
        string[] Aliases { get; }
        string Description { get; }
        bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response);
    }
    public sealed class ClientCommandHandler { }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CommandHandlerAttribute(Type handler) : Attribute
    {
        public Type Handler { get; } = handler;
    }
}

namespace PlayerRoles
{
    public enum Faction { FoundationStaff, FoundationEnemy, SCP, Unclassified }
    public enum RoleTypeId
    {
        None, Spectator, ClassD, Scientist, FacilityGuard, NtfPrivate, NtfSergeant,
        NtfSpecialist, NtfCaptain, ChaosConscript, ChaosRifleman, ChaosMarauder,
        ChaosRepressor, Tutorial, Overwatch, Filmmaker, Scp173
    }
}

namespace LabApi.Features
{
    public static class LabApiProperties
    {
        public const string CompiledVersion = "1.1.7";
    }
}

namespace LabApi.Loader.Features.Plugins
{
    public abstract class Plugin<T> where T : class, new()
    {
        public T? Config { get; set; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract string Author { get; }
        public abstract Version Version { get; }
        public abstract Version RequiredApiVersion { get; }
        public virtual string ConfigFileName { get; set; } = "config.yml";
        public abstract void Enable();
        public abstract void Disable();
    }
}

namespace LabApi.Features.Console
{
    public static class Logger
    {
        public static readonly List<string> Messages = new();
        public static void Info(string text) => Messages.Add(text);
        public static void Warn(string text) => Messages.Add(text);
    }
}

namespace LabApi.Features.Wrappers
{
    public sealed class Player : CommandSystem.ICommandSender
    {
        public static readonly List<Player> ReadyList = new();
        public static bool TryGet(CommandSystem.ICommandSender sender, out Player player)
        {
            player = sender as Player ?? null!;
            return player != null;
        }
        public PlayerRoles.Faction Faction { get; set; }
        public PlayerRoles.RoleTypeId Role { get; set; }
        public bool IsAlive { get; set; } = true;
        public bool IsReady { get; set; } = true;
        public bool IsMuted { get; set; }
        public bool RemoteAdminAccess { get; set; }
        public string? DisplayName { get; set; } = "测试玩家";
        public string? Nickname { get; set; } = "测试玩家";
        public string LogName => Nickname + " (test@steam)";
        public readonly List<(string Text, ushort Duration)> Broadcasts = new();
        public readonly List<(string Text, float Duration)> Hints = new();
        public readonly List<string> ConsoleMessages = new();
        public void SendBroadcast(string text, ushort duration, bool shouldClearPrevious = false)
            => Broadcasts.Add((text, duration));
        public void SendHint(string text, float duration) => Hints.Add((text, duration));
        public void SendConsoleMessage(string text, string color) => ConsoleMessages.Add(text);
    }
}

namespace LabApi.Events.Arguments.PlayerEvents
{
    public sealed class PlayerReportedPlayerEventArgs(LabApi.Features.Wrappers.Player player, LabApi.Features.Wrappers.Player target, string reason)
    {
        public LabApi.Features.Wrappers.Player Player { get; } = player;
        public LabApi.Features.Wrappers.Player Target { get; } = target;
        public string Reason { get; } = reason;
    }

    public sealed class PlayerReportedCheaterEventArgs(LabApi.Features.Wrappers.Player player, LabApi.Features.Wrappers.Player target, string reason)
    {
        public LabApi.Features.Wrappers.Player Player { get; } = player;
        public LabApi.Features.Wrappers.Player Target { get; } = target;
        public string Reason { get; } = reason;
    }
}

namespace LabApi.Events.Handlers
{
    public static class PlayerEvents
    {
        public static event Action<LabApi.Events.Arguments.PlayerEvents.PlayerReportedPlayerEventArgs>? ReportedPlayer;
        public static event Action<LabApi.Events.Arguments.PlayerEvents.PlayerReportedCheaterEventArgs>? ReportedCheater;
        public static void RaiseReportedPlayer(LabApi.Features.Wrappers.Player player, LabApi.Features.Wrappers.Player target, string reason)
            => ReportedPlayer?.Invoke(new(player, target, reason));
        public static void RaiseReportedCheater(LabApi.Features.Wrappers.Player player, LabApi.Features.Wrappers.Player target, string reason)
            => ReportedCheater?.Invoke(new(player, target, reason));
    }
}
