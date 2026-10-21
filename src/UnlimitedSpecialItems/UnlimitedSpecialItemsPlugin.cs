using System;
using System.IO;
using System.Reflection;
using InventorySystem.Configs;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader;
using LabApi.Loader.Features.Plugins;

namespace UnlimitedSpecialItems;

/// <summary>
/// Lift the special-weapon cap; the game's eight-item inventory stays intact.
/// The game clamps category limits to [-8, 8] and compares their absolute value.
/// A negative default also prevents server config from reinstating a lower cap.
/// </summary>
public sealed class UnlimitedSpecialItemsPlugin : Plugin
{
    private sbyte _oldSpecialWeapon;
    private sbyte _oldScpItem;
    private bool _enabled;
    private bool _scpOverridden;

    public override string Name => "UnlimitedSpecialItems";
    public override string Description => "解除特殊武器单独携带上限；SCP 物品可选解除";
    public override string Author => "Codex";
    public override Version Version => new(1, 1, 0);
    public override Version RequiredApiVersion => new(LabApiProperties.CompiledVersion);
    public Config Config { get; private set; } = new();

    public override void LoadConfigs()
    {
        Config = this.LoadConfig<Config>("config.yml") ??
            throw new InvalidDataException("UnlimitedSpecialItems/config.yml 读取失败");
        base.LoadConfigs();
    }

    public override void Enable()
    {
        if (!Config.IsEnabled) return;

        var limits = InventoryLimits.StandardCategoryLimits;
        _oldSpecialWeapon = limits.TryGetValue(ItemCategory.SpecialWeapon, out var special) ? special : (sbyte)8;
        _oldScpItem = limits.TryGetValue(ItemCategory.SCPItem, out var scp) ? scp : (sbyte)8;
        limits[ItemCategory.SpecialWeapon] = -8;
        _scpOverridden = Config.UnlimitedScpItems;
        if (_scpOverridden) limits[ItemCategory.SCPItem] = -8;
        _enabled = true;
        ServerEvents.WaitingForPlayers += RefreshLimits;
        ServerEvents.RoundStarted += RefreshLimits;
        RefreshLimits();
        Logger.Info(_scpOverridden ? "特殊武器与 SCP 物品类别上限已放宽至 8，背包仍为 8 格。" :
            "特殊武器上限已放宽至 8；SCP 物品、枪械和药物由 config_gameplay.txt 控制。");
    }

    public override void Disable()
    {
        if (!_enabled) return;
        ServerEvents.WaitingForPlayers -= RefreshLimits;
        ServerEvents.RoundStarted -= RefreshLimits;
        var limits = InventoryLimits.StandardCategoryLimits;
        limits[ItemCategory.SpecialWeapon] = _oldSpecialWeapon;
        if (_scpOverridden) limits[ItemCategory.SCPItem] = _oldScpItem;
        _enabled = false;
        _scpOverridden = false;
        RefreshLimits();
    }

    private static void RefreshLimits()
    {
        // The singleton can be unavailable during early plugin loading. The
        // WaitingForPlayers callback applies the limits when the server starts.
        try
        {
            var type = typeof(InventoryLimits).Assembly.GetType("ServerConfigSynchronizer");
            var singleton = type?.GetField("Singleton", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (singleton == null) return;
            type?.GetMethod("RefreshCategoryLimits", BindingFlags.Public | BindingFlags.Instance)
                ?.Invoke(singleton, null);
        }
        catch (Exception ex) { Logger.Warn($"同步物品容量失败，等待下一次回合：{ex.Message}"); }
    }
}
