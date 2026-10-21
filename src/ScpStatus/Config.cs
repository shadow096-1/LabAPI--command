using System.Collections.Generic;

namespace ScpStatus;

public sealed class Config
{
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, float> FixedHealth { get; set; } = new();
    public float SecondsBeforeRegen { get; set; } = 5f;
    public float RegenPerSecond { get; set; } = 5f;
    public float MovementThreshold { get; set; } = 0.08f;
    public bool ShowTeamHud { get; set; } = true;
    public int HudFontSize { get; set; } = 17;
    public float HudX { get; set; } = 570f;
    public float HudY { get; set; } = 170f;
    public bool NativeHudFallback { get; set; } = true;
}
