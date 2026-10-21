using System;
using System.Linq;
using System.Reflection;
using LabApi.Features.Wrappers;

namespace ScpStatus;

// Reflection keeps the plugin loadable when HintServiceMeow is not installed.
internal sealed class HintBridge
{
    private Type? _hintType;
    private MethodInfo? _getDisplay;
    private MethodInfo? _add;
    private MethodInfo? _remove;

    private bool Initialize()
    {
        if (_hintType != null) return true;
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "HintServiceMeow");
        var hint = assembly?.GetType("HintServiceMeow.Core.Models.Hints.Hint");
        var display = assembly?.GetType("HintServiceMeow.Core.Utilities.PlayerDisplay");
        if (hint == null || display == null) return false;
        var get = display.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "Get" && m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(Player));
        var add = display.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "AddHint" && m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType.IsAssignableFrom(hint));
        var remove = display.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "RemoveHint" && m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType.IsAssignableFrom(hint));
        if (get == null || add == null || remove == null) return false;
        _getDisplay = get;
        _add = add;
        _remove = remove;
        _hintType = hint;
        return true;
    }

    public object? Add(Player player, string text, int size, float x, float y)
    {
        try
        {
            if (!Initialize()) return null;
            var hint = Activator.CreateInstance(_hintType!)!;
            _hintType!.GetProperty("Text")!.SetValue(hint, text);
            _hintType.GetProperty("FontSize")!.SetValue(hint, size);
            _hintType.GetProperty("XCoordinate")!.SetValue(hint, x);
            _hintType.GetProperty("YCoordinate")!.SetValue(hint, y);
            var align = _hintType.GetProperty("Alignment")!;
            align.SetValue(hint, Enum.Parse(align.PropertyType, "Right"));
            _add!.Invoke(_getDisplay!.Invoke(null, new object[] { player }), new[] { hint });
            return hint;
        }
        catch (Exception) { return null; }
    }

    public bool Update(object hint, string text)
    {
        try { _hintType!.GetProperty("Text")!.SetValue(hint, text); return true; }
        catch (Exception) { return false; }
    }

    public void Remove(Player player, object hint)
    {
        try { _remove?.Invoke(_getDisplay?.Invoke(null, new object[] { player }), new[] { hint }); }
        catch (Exception) { }
    }
}
