using System;
using System.Linq;
using System.Reflection;
using LabApi.Features.Wrappers;

namespace ScpStatus;

internal static class HudRouter
{
    private static MethodInfo? _set;
    private static MethodInfo? _clear;

    private static void Find()
    {
        if (_set != null && _clear != null) return;
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "PlayerLevels")?.GetType("PlayerLevels.HudBus");
        _set = type?.GetMethod("TrySet", BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(Player), typeof(string), typeof(string), typeof(float) }, null);
        _clear = type?.GetMethod("TryClear", BindingFlags.Public | BindingFlags.Static,
            null, new[] { typeof(Player), typeof(string) }, null);
    }

    public static bool TrySet(Player viewer, string content, int fontSize)
    {
        try
        {
            Find();
            return _set != null && (bool)_set.Invoke(null,
                new object[] { viewer, "scp", $"<size={fontSize}>{content}</size>", 1.8f })!;
        }
        catch (Exception) { _set = null; return false; }
    }

    public static void Clear(Player viewer)
    {
        try { Find(); _clear?.Invoke(null, new object[] { viewer, "scp" }); }
        catch (Exception) { _clear = null; }
    }
}
