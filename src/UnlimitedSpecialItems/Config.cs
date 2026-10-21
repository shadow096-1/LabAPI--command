namespace UnlimitedSpecialItems;

public sealed class Config
{
    public bool IsEnabled { get; set; } = true;
    // SCP item limits can be set directly in config_gameplay.txt.
    public bool UnlimitedScpItems { get; set; } = false;
}
