namespace PlayerLevels;

public sealed class Config
{
    public bool IsEnabled { get; set; } = true;
    public int RoundPoints { get; set; } = 2;
    public int EscapePoints { get; set; } = 5;
    public int MainScpKillPoints { get; set; } = 6;
    public int ZombieKillPoints { get; set; } = 3;
    public int HostileKillPoints { get; set; } = 2;
    public int MaxAdminMultiplier { get; set; } = 100;
    public float HudY { get; set; } = 1080f;
    public int HudFontSize { get; set; } = 14;
    public float HudRefreshSeconds { get; set; } = 2f;
    public int HudNativeVerticalOffset { get; set; } = -220;
    public int HudLeadingLines { get; set; } = 17;
}
