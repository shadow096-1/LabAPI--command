using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using MEC;
using PlayerRoles;

namespace PlayerLevels;

public sealed class PlayerLevelsPlugin : Plugin
{
    public static PlayerLevelsPlugin? Instance { get; private set; }
    public override string Name => "PlayerLevels";
    public override string Description => "回合、撤离与敌对击杀经验；两行等级界面与管理员经验倍数";
    public override string Author => "Codex";
    public override Version Version => new(1, 1, 0);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    public Config Config { get; private set; } = new();

    private static readonly Regex Prefix = new(@"^\[Lv\.\d+\](?:\[(?:基金会|混沌|D级|SCP|观察|未知|火烈鸟)\])?", RegexOptions.Compiled);
    private readonly HashSet<string> _participants = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _escaped = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _sessionStarted = new(StringComparer.OrdinalIgnoreCase);
    private LevelStore _store = new();
    private string _levelsPath = "";
    private string _multipliersPath = "";
    private bool _running;
    private bool _roundAwarded;
    private int _loopGeneration;

    public override void LoadConfigs()
    {
        Config = this.LoadConfig<Config>("config.yml") ??
            throw new InvalidDataException("PlayerLevels/config.yml 读取失败");
        // Existing 1.0.0 configs contain the old mid-screen placement.
        var migrated = false;
        if (Config.HudY == 850f || Config.HudY == 1180f || Config.HudY == 1260f)
        {
            Config.HudY = 1080f;
            migrated = true;
        }
        if (Config.HudFontSize == 22 || Config.HudFontSize == 16)
        {
            Config.HudFontSize = 14;
            migrated = true;
        }
        if (Config.HudLeadingLines == 14)
        {
            Config.HudLeadingLines = 17;
            migrated = true;
        }
        if (migrated) this.SaveConfig(Config, "config.yml");
        base.LoadConfigs();
    }

    public override void Enable()
    {
        if (!Config.IsEnabled) return;
        var dataDirectory = this.GetConfigDirectory().FullName;
        _levelsPath = Path.Combine(dataDirectory, "players.txt");
        _multipliersPath = Path.Combine(dataDirectory, "multipliers.txt");
        _store = LevelStore.Load(_levelsPath, _multipliersPath);
        Instance = this;
        _running = true;
        var generation = ++_loopGeneration;
        PlayerEvents.Spawned += OnSpawned;
        PlayerEvents.Left += OnLeft;
        PlayerEvents.Death += OnDeath;
        PlayerEvents.Escaped += OnEscaped;
        ServerEvents.RoundStarted += OnRoundStarted;
        ServerEvents.RoundEnded += OnRoundEnded;
        if (Round.IsRoundStarted && !Round.IsRoundEnded)
            foreach (var player in Player.ReadyList) MarkParticipating(player);
        Timing.RunCoroutine(RefreshHud(generation));
        Logger.Info($"等级插件已加载：{_store.Players.Count} 条玩家记录。");
    }

    public override void Disable()
    {
        _running = false;
        ++_loopGeneration;
        PlayerEvents.Spawned -= OnSpawned;
        PlayerEvents.Left -= OnLeft;
        PlayerEvents.Death -= OnDeath;
        PlayerEvents.Escaped -= OnEscaped;
        ServerEvents.RoundStarted -= OnRoundStarted;
        ServerEvents.RoundEnded -= OnRoundEnded;
        foreach (var player in Player.ReadyList)
        {
            RemovePrefix(player);
        }
        _sessionStarted.Clear();
        Instance = null;
    }

    private void OnRoundStarted()
    {
        _participants.Clear();
        _escaped.Clear();
        _roundAwarded = false;
        foreach (var player in Player.ReadyList) MarkParticipating(player);
    }

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        if (_roundAwarded) return;
        _roundAwarded = true;
        var count = 0;
        foreach (var userId in _participants)
        {
            if (string.IsNullOrWhiteSpace(userId)) continue;
            ApplyPoints(userId, Config.RoundPoints);
            count++;
        }
        SaveLevels();
        Logger.Info($"本局参与积分已发放给 {count} 名玩家。");
    }

    private static bool IsPlayedRole(Player player) => player.IsReady && player.IsPlayer &&
        player.IsAlive && player.Role.GetFaction() is
            Faction.FoundationStaff or Faction.FoundationEnemy or Faction.SCP;

    private void MarkParticipating(Player player)
    {
        if (IsPlayedRole(player) && !string.IsNullOrWhiteSpace(player.UserId))
            _participants.Add(player.UserId);
    }

    private void OnSpawned(PlayerSpawnedEventArgs ev)
    {
        _escaped.Remove(ev.Player.UserId);
        if (Round.IsRoundStarted && !Round.IsRoundEnded) MarkParticipating(ev.Player);
    }

    private void OnLeft(PlayerLeftEventArgs ev)
    {
        RemovePrefix(ev.Player);
        _sessionStarted.Remove(ev.Player.UserId);
        // A player who played and disconnected still earns the round points.
    }

    private void OnEscaped(PlayerEscapedEventArgs ev)
    {
        if (ev.OldRole is not (RoleTypeId.ClassD or RoleTypeId.Scientist)) return;
        _escaped.Add(ev.Player.UserId);
        Award(ev.Player.UserId, Config.EscapePoints);
    }

    private void OnDeath(PlayerDeathEventArgs ev)
    {
        var attacker = ev.Attacker;
        if (attacker == null || ev.Player == null || attacker == ev.Player ||
            !attacker.IsPlayer || !attacker.IsReady ||
            string.IsNullOrWhiteSpace(attacker.UserId)) return;

        var attackerFaction = attacker.Role.GetFaction();
        var victimFaction = ev.OldRole.GetFaction();
        if (attackerFaction == victimFaction ||
            attackerFaction == Faction.Unclassified || victimFaction == Faction.Unclassified)
            return;

        var amount = ev.OldRole == RoleTypeId.Scp0492 ? Config.ZombieKillPoints :
            victimFaction == Faction.SCP ? Config.MainScpKillPoints :
            victimFaction is Faction.FoundationStaff or Faction.FoundationEnemy
                ? Config.HostileKillPoints : 0;
        if (amount > 0) Award(attacker.UserId, amount);
    }

    private void Award(string userId, int basePoints)
    {
        ApplyPoints(userId, basePoints);
        SaveLevels();
    }

    private void ApplyPoints(string userId, int basePoints)
    {
        if (basePoints <= 0) return;
        var record = _store.Get(userId);
        var multiplier = EffectiveMultiplier(userId);
        var amount = (long)basePoints * multiplier;
        record.TotalPoints += amount;
        var remaining = record.Experience + amount;
        while (record.Level < int.MaxValue && remaining >= LevelStore.Requirement(record.Level))
        {
            remaining -= LevelStore.Requirement(record.Level);
            record.Level++;
        }
        record.Experience = (int)Math.Min(remaining, LevelStore.Requirement(record.Level));
    }

    private int EffectiveMultiplier(string userId) =>
        Math.Max(1, Math.Min(Math.Max(1, Config.MaxAdminMultiplier), _store.GetMultiplier(userId)));

    private void SaveLevels()
    {
        if (string.IsNullOrEmpty(_levelsPath)) return;
        try { _store.SaveLevels(_levelsPath); }
        catch (Exception ex) { Logger.Error($"保存等级数据失败：{ex}"); }
    }

    private bool SaveMultipliers()
    {
        if (string.IsNullOrEmpty(_multipliersPath)) return false;
        try { _store.SaveMultipliers(_multipliersPath); return true; }
        catch (Exception ex) { Logger.Error($"保存经验倍数失败：{ex}"); return false; }
    }

    public string SetMultiplier(string userId, int multiplier)
    {
        if (multiplier < 1 || multiplier > Math.Max(1, Config.MaxAdminMultiplier))
            return $"倍数须为 1 到 {Config.MaxAdminMultiplier} 的整数。";
        int oldMultiplier = _store.GetMultiplier(userId);
        int level = _store.Players.TryGetValue(userId, out var record) ? record.Level : 1;
        _store.SetMultiplier(userId, multiplier);
        if (!SaveMultipliers())
        {
            _store.SetMultiplier(userId, oldMultiplier);
            return "经验倍数保存失败，请检查服务器日志和文件权限。";
        }
        return $"{userId} 的经验倍数已设为 {multiplier} 倍；当前 Lv.{level}。";
    }

    public string GetMultiplier(string userId)
    {
        var record = _store.Players.TryGetValue(userId, out var existing) ? existing : new LevelRecord();
        return $"{userId}：Lv.{record.Level}，经验 {record.Experience}/{LevelStore.Requirement(record.Level)}，{EffectiveMultiplier(userId)} 倍。";
    }

    public string ReloadRecords()
    {
        try
        {
            var loaded = LevelStore.Load(_levelsPath, _multipliersPath);
            _store = loaded;
            return $"已重新载入 players.txt（{loaded.Players.Count} 人）和 multipliers.txt（{loaded.Multipliers.Count} 条）。";
        }
        catch (Exception ex)
        {
            return $"载入失败，当前数据未改变：{ex.Message}";
        }
    }

    private IEnumerator<float> RefreshHud(int generation)
    {
        while (_running && generation == _loopGeneration)
        {
            foreach (var player in Player.ReadyList)
            {
                if (!player.IsPlayer || string.IsNullOrWhiteSpace(player.UserId)) continue;
                if (!_sessionStarted.ContainsKey(player.UserId))
                    _sessionStarted[player.UserId] = DateTime.UtcNow;
                bool isNew = !_store.Players.ContainsKey(player.UserId);
                var record = _store.Get(player.UserId);
                if (isNew) SaveLevels();
                RefreshPrefix(player, record.Level);
                RefreshHint(player, record);
            }
            yield return Timing.WaitForSeconds(Math.Max(0.5f, Config.HudRefreshSeconds));
        }
    }

    private static void RefreshPrefix(Player player, int level)
    {
        var current = player.DisplayName;
        // DisplayName can contain formatting injected by another plugin. Reusing
        // it every refresh appends fragments of rich-text tags to the nickname.
        var name = player.Nickname;
        var intended = $"[Lv.{level}]{name}";
        if (current != intended) player.DisplayName = intended;
    }

    private static void RemovePrefix(Player player)
    {
        try
        {
            var current = player.DisplayName;
            if (Prefix.IsMatch(current)) player.DisplayName = player.Nickname;
        }
        catch (Exception) { /* A disconnected player's name may be gone. */ }
    }

    private void RefreshHint(Player player, LevelRecord record)
    {
        var safeName = EscapeText(player.Nickname);
        var roleName = RoleName(player.Role);
        var factionName = FactionName(player.Role);
        var state = _escaped.Contains(player.UserId) && !player.IsAlive ? "已撤离" :
            player.IsAlive ? "存活" : "等待复活";
        var elapsed = DateTime.UtcNow - _sessionStarted[player.UserId];
        var playTime = $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        int multiplier = EffectiveMultiplier(player.UserId);
        var text = $"<color=#EDEDED>欢迎 {safeName}｜阵营：{factionName}｜角色：{roleName}｜状态：{state}</color>\n" +
                   $"<color=#72D7FF>等级 Lv.{record.Level}　经验 {record.Experience}/{LevelStore.Requirement(record.Level)}</color>" +
                   $" <color=#C7EBB4>游玩 {playTime}</color>" +
                   (multiplier > 1 ? $" <color=#FFD36A>×{multiplier}</color>" : "");
        // The native renderer expands line spacing when voffset spans a newline.
        // Padding before the small HUD block moves both lines together instead.
        int size = Math.Max(10, Math.Min(28, Config.HudFontSize));
        int padding = Math.Max(0, Math.Min(20, Config.HudLeadingLines));
        player.SendHint(new string('\n', padding) + $"<align=center><size={size}>{text}</size></align>",
            Math.Max(1.5f, Config.HudRefreshSeconds + 0.4f));
    }

    private static string EscapeText(string text) => (text ?? "")
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string FactionName(RoleTypeId role)
    {
        if (role == RoleTypeId.ClassD) return "D级";
        return role.GetFaction() switch
        {
            Faction.FoundationStaff => "基金会",
            Faction.FoundationEnemy => "混沌",
            Faction.SCP => "SCP",
            Faction.Flamingos => "火烈鸟",
            _ => "观察"
        };
    }

    private static string RoleName(RoleTypeId role) => role switch
    {
        RoleTypeId.ClassD => "D级人员",
        RoleTypeId.Scientist => "科学家",
        RoleTypeId.FacilityGuard => "设施警卫",
        RoleTypeId.Spectator => "观察者",
        RoleTypeId.NtfCaptain => "九尾狐队长",
        RoleTypeId.NtfSergeant => "九尾狐中士",
        RoleTypeId.NtfPrivate => "九尾狐列兵",
        RoleTypeId.NtfSpecialist => "九尾狐专家",
        RoleTypeId.ChaosConscript => "混沌征召兵",
        RoleTypeId.ChaosRifleman => "混沌步枪兵",
        RoleTypeId.ChaosMarauder => "混沌掠夺者",
        RoleTypeId.ChaosRepressor => "混沌镇压者",
        RoleTypeId.Scp0492 => "SCP-049-2",
        _ => role.ToString()
    };
}
