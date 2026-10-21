using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LabApi.Features.Wrappers;

namespace PlayerLevels;

/// <summary>
/// One native SendHint writer for the level, chat and SCP status plugins.
/// The other DLLs call TrySet by reflection and do not need an extra dependency.
/// </summary>
public static class HudBus
{
    private sealed class Slot
    {
        public string Text = "";
        public DateTime Expires;
    }

    private static readonly Dictionary<Player, Dictionary<string, Slot>> Active = new();
    private static bool _enabled;
    private static int _leadingLines = 17;

    internal static void Start(int leadingLines)
    {
        _leadingLines = Math.Max(5, Math.Min(20, leadingLines));
        _enabled = true;
    }

    internal static void Stop()
    {
        _enabled = false;
        Active.Clear();
    }

    public static bool TrySet(Player viewer, string zone, string markup, float seconds)
    {
        if (!_enabled || viewer == null || !viewer.IsReady ||
            zone is not ("levels" or "chat" or "scp")) return false;
        if (!Active.TryGetValue(viewer, out var slots))
            Active[viewer] = slots = new Dictionary<string, Slot>(StringComparer.Ordinal);
        slots[zone] = new Slot
        {
            Text = markup,
            Expires = DateTime.UtcNow.AddSeconds(Math.Max(0.5f, seconds))
        };
        return true;
    }

    public static bool TryClear(Player viewer, string zone)
    {
        if (!_enabled) return false;
        if (Active.TryGetValue(viewer, out var slots)) slots.Remove(zone);
        return true;
    }

    internal static void Forget(Player viewer) => Active.Remove(viewer);

    internal static void Render()
    {
        if (!_enabled) return;
        var now = DateTime.UtcNow;
        foreach (var pair in Active.ToArray())
        {
            var viewer = pair.Key;
            var slots = pair.Value;
            foreach (var expired in slots.Where(s => s.Value.Expires <= now).Select(s => s.Key).ToArray())
                slots.Remove(expired);
            if (!viewer.IsReady || slots.Count == 0)
            {
                Active.Remove(viewer);
                continue;
            }

            string chat = slots.TryGetValue("chat", out var c) ? c.Text : "";
            string scp = slots.TryGetValue("scp", out var s) ? s.Text : "";
            string levels = slots.TryGetValue("levels", out var l) ? l.Text : "";
            var result = new StringBuilder();
            result.Append("<align=left>").Append(chat).Append("</align>\n");
            int rows = 1;
            if (!string.IsNullOrEmpty(scp))
            {
                result.Append("<align=right>").Append(scp).Append("</align>\n");
                rows += scp.Count(ch => ch == '\n') + 1;
            }
            result.Append('\n', Math.Max(0, _leadingLines - rows));
            result.Append("<align=center>").Append(levels).Append("</align>");
            viewer.SendHint(result.ToString(), 1.1f);
        }
    }
}
