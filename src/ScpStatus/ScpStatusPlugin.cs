using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;
using MEC;
using PlayerRoles;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace ScpStatus;

public sealed class ScpStatusPlugin : Plugin
{
    private sealed class MovementState
    {
        public RoleTypeId Role;
        public Vector3 Position;
        public DateTime LastMovement;
        public DateTime LastHeal;
    }

    public static ScpStatusPlugin? Instance { get; private set; }
    public override string Name => "ScpStatus";
    public override string Description => "SCP 固定血量、静止回血与右上角队友状态";
    public override string Author => "Codex";
    public override Version Version => new(1, 1, 0);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    public Config Config { get; private set; } = new();

    private readonly Dictionary<Player, MovementState> _movement = new();
    private readonly Dictionary<Player, object> _teamHints = new();
    private readonly HintBridge _hintBridge = new();
    private bool _running;
    private bool _loggedNativeFallback;
    private int _generation;

    public override void LoadConfigs()
    {
        Config = this.LoadConfig<Config>("config.yml") ??
            throw new InvalidDataException("ScpStatus/config.yml 读取失败");
        Config.FixedHealth ??= new Dictionary<string, float>();
        base.LoadConfigs();
    }

    public override void Enable()
    {
        if (!Config.IsEnabled) return;
        Instance = this;
        _running = true;
        _loggedNativeFallback = false;
        int generation = ++_generation;
        PlayerEvents.Spawned += OnSpawned;
        PlayerEvents.Dying += OnDying;
        PlayerEvents.Left += OnLeft;
        foreach (var player in Player.ReadyList)
            InitializePlayer(player, applyHealth: true);
        Timing.RunCoroutine(Tick(generation));
        Logger.Info("SCP 状态已启用；管理员输入 scphp list 查看固定血量设置。");
    }

    public override void Disable()
    {
        _running = false;
        ++_generation;
        PlayerEvents.Spawned -= OnSpawned;
        PlayerEvents.Dying -= OnDying;
        PlayerEvents.Left -= OnLeft;
        foreach (var entry in _teamHints)
            _hintBridge.Remove(entry.Key, entry.Value);
        foreach (var viewer in Player.ReadyList) HudRouter.Clear(viewer);
        _teamHints.Clear();
        _movement.Clear();
        Instance = null;
    }

    private void OnSpawned(PlayerSpawnedEventArgs ev)
    {
        _movement.Remove(ev.Player);
        RemoveTeamHint(ev.Player);
        // Spawned can run before the role's default health has been assigned.
        Timing.CallDelayed(0.35f, () => { if (_running) InitializePlayer(ev.Player, applyHealth: true); });
    }

    private void OnDying(PlayerDyingEventArgs ev)
    {
        _movement.Remove(ev.Player);
        RemoveTeamHint(ev.Player);
    }

    private void OnLeft(PlayerLeftEventArgs ev)
    {
        _movement.Remove(ev.Player);
        RemoveTeamHint(ev.Player);
    }

    private void InitializePlayer(Player player, bool applyHealth)
    {
        if (!player.IsReady || !player.IsAlive || !IsScp(player.Role)) return;
        var now = DateTime.UtcNow;
        _movement[player] = new MovementState
        {
            Role = player.Role,
            Position = player.Position,
            LastMovement = now,
            LastHeal = now
        };
        if (applyHealth) ApplyHealth(player);
    }

    private void ApplyHealth(Player player)
    {
        if (player.Role == RoleTypeId.Scp079 || !IsScp(player.Role)) return;
        if (!Config.FixedHealth.TryGetValue(player.Role.ToString(), out float maximum) || maximum <= 0)
            return;
        player.MaxHealth = maximum;
        player.Health = maximum;
    }

    private IEnumerator<float> Tick(int generation)
    {
        int iterations = 0;
        while (_running && generation == _generation)
        {
            DateTime now = DateTime.UtcNow;
            foreach (var player in Player.ReadyList)
            {
                if (!player.IsReady || !player.IsAlive || !IsScp(player.Role))
                {
                    _movement.Remove(player);
                    RemoveTeamHint(player);
                    continue;
                }

                if (!_movement.TryGetValue(player, out var state) || state.Role != player.Role)
                {
                    InitializePlayer(player, applyHealth: true);
                    continue;
                }

                if (player.Role != RoleTypeId.Scp079 && player.MaxHealth > 0)
                {
                    Vector3 position = player.Position;
                    float threshold = Math.Max(0.02f, Config.MovementThreshold);
                    if ((position - state.Position).sqrMagnitude > threshold * threshold)
                    {
                        state.LastMovement = now;
                        state.LastHeal = now;
                        state.Position = position;
                    }
                    if ((now - state.LastMovement).TotalSeconds >= Math.Max(1f, Config.SecondsBeforeRegen) &&
                        (now - state.LastHeal).TotalSeconds >= 1.0)
                    {
                        player.Health = Math.Min(player.MaxHealth,
                            player.Health + Math.Max(0f, Config.RegenPerSecond));
                        state.LastHeal = now;
                    }
                }
            }

            if (++iterations % 4 == 0 && Config.ShowTeamHud)
                RefreshTeamHud();
            yield return Timing.WaitForSeconds(0.25f);
        }
    }

    private void RefreshTeamHud()
    {
        var players = Player.ReadyList.Where(p => p.IsReady && p.IsAlive && IsScp(p.Role)).ToList();
        int zombies = players.Count(p => p.Role == RoleTypeId.Scp0492);
        var main = players.Where(p => p.Role != RoleTypeId.Scp0492).ToList();
        var teamText = BuildTeamText(main, zombies);
        foreach (var viewer in players)
        {
            if (HudRouter.TrySet(viewer, teamText, Math.Max(12, Math.Min(28, Config.HudFontSize))))
            {
                if (_teamHints.TryGetValue(viewer, out var oldHint))
                {
                    _hintBridge.Remove(viewer, oldHint);
                    _teamHints.Remove(viewer);
                }
                continue;
            }
            if (_teamHints.TryGetValue(viewer, out var hint) && _hintBridge.Update(hint, teamText))
                continue;
            RemoveTeamHint(viewer);
            var added = _hintBridge.Add(viewer, teamText, Config.HudFontSize, Config.HudX, Config.HudY);
            if (added != null)
            {
                _teamHints[viewer] = added;
                continue;
            }

            if (!Config.NativeHudFallback) continue;
            if (!_loggedNativeFallback)
            {
                _loggedNativeFallback = true;
                Logger.Warn("HintServiceMeow 未加载，SCP 队友栏改用游戏提示通道，可能与其他原生提示互相覆盖。");
            }
            int font = Math.Max(12, Math.Min(28, Config.HudFontSize));
            viewer.SendHint($"<align=right><size={font}>{teamText}</size></align>" + new string('\n', 12), 1.3f);
        }
    }

    private void RemoveTeamHint(Player player)
    {
        HudRouter.Clear(player);
        if (!_teamHints.TryGetValue(player, out var hint)) return;
        _hintBridge.Remove(player, hint);
        _teamHints.Remove(player);
    }

    private static string BuildTeamText(List<Player> main, int zombies)
    {
        var builder = new StringBuilder();
        builder.Append("<color=#FF4141><b>SCP 阵营｜存活 ").Append(main.Count + zombies).Append("</b></color>");
        foreach (var player in main.OrderBy(p => p.Role.ToString()))
        {
            builder.Append('\n').Append("<color=#FF6666>").Append(FormatRole(player.Role)).Append("</color>  ");
            if (player.MaxHealth > 0)
                builder.Append("<color=#EF9FEF>").Append(Math.Ceiling(player.Health)).Append('/')
                    .Append(Math.Ceiling(player.MaxHealth)).Append(" HP</color>");
            else
                builder.Append("<color=#AAAAAA>在线</color>");
            if (player.HumeShield > 0)
                builder.Append(" <color=#73E8CE>HS ").Append(Math.Ceiling(player.HumeShield)).Append("</color>");
        }
        builder.Append('\n').Append("<color=#FF6666>SCP-049-2</color>  ×").Append(zombies);
        return builder.ToString();
    }

    private static string FormatRole(RoleTypeId role)
    {
        string name = role.ToString();
        return "SCP-" + name.Substring(3).Replace("0492", "049-2");
    }

    internal static bool IsScp(RoleTypeId role) =>
        role.ToString().StartsWith("Scp", StringComparison.OrdinalIgnoreCase);

    public static bool TryParseScp(string input, out RoleTypeId role)
    {
        string normalized = input.Trim().Replace("-", "").Replace("_", "");
        if (!normalized.StartsWith("Scp", StringComparison.OrdinalIgnoreCase))
            normalized = "Scp" + normalized;
        return Enum.TryParse(normalized, true, out role) && IsScp(role) && role != RoleTypeId.Scp079;
    }

    public string SetHealth(RoleTypeId role, float? maximum)
    {
        if (maximum.HasValue) Config.FixedHealth[role.ToString()] = maximum.Value;
        else Config.FixedHealth.Remove(role.ToString());
        this.SaveConfig(Config, "config.yml");
        if (maximum.HasValue)
        {
            foreach (var player in Player.ReadyList.Where(p => p.IsReady && p.IsAlive && p.Role == role))
                ApplyHealth(player);
        }
        return maximum.HasValue ? $"{FormatRole(role)} 固定血量已设为 {maximum.Value:0.##}。" :
            $"{FormatRole(role)} 已恢复游戏默认血量（当前角色下次生成时生效）。";
    }

    public string ListHealth() => Config.FixedHealth.Count == 0 ? "当前未设置固定血量。" :
        string.Join("；", Config.FixedHealth.OrderBy(pair => pair.Key)
            .Select(pair => pair.Key + "=" + pair.Value.ToString("0.##")));
}
