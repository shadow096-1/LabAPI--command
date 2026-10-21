using System;
using System.Linq;
using System.Reflection;
using LabApi.Features.Wrappers;

namespace SLTranslateCN;

// Optional bridge: no compile-time dependency on the level plugin.
internal static class HudRouter
{
    private static MethodInfo? _set;

    public static bool TrySet(Player viewer, string markup, float seconds)
    {
        try
        {
            if (_set == null)
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "PlayerLevels");
                _set = assembly?.GetType("PlayerLevels.HudBus")?.GetMethod("TrySet",
                    BindingFlags.Public | BindingFlags.Static,
                    null, new[] { typeof(Player), typeof(string), typeof(string), typeof(float) }, null);
            }
            return _set != null && (bool)_set.Invoke(null, new object[] { viewer, "chat", markup, seconds })!;
        }
        catch (Exception)
        {
            _set = null;
            return false;
        }
    }
}
