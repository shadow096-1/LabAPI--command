using System;
using System.Linq;
using System.Reflection;
using LabApi.Features.Wrappers;

namespace SLTranslateCN;

// Discovered at runtime so servers without HSM can still load the chat plugin.
internal sealed class ChatHintBridge
{
    private Type? _hintType;
    private MethodInfo? _getDisplay;
    private MethodInfo? _show;

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
        var show = display.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "ShowHint" &&
                (m.GetParameters().Length == 2 || m.GetParameters().Length == 3) &&
                m.GetParameters()[0].ParameterType.IsAssignableFrom(hint) &&
                m.GetParameters()[1].ParameterType == typeof(float));
        if (get == null || show == null) return false;
        _getDisplay = get;
        _show = show;
        _hintType = hint;
        return true;
    }

    public bool Show(Player player, string text, int size, float seconds)
    {
        try
        {
            if (!Initialize()) return false;
            var hint = Activator.CreateInstance(_hintType!)!;
            _hintType!.GetProperty("Text")!.SetValue(hint, text);
            _hintType.GetProperty("FontSize")!.SetValue(hint, size);
            _hintType.GetProperty("XCoordinate")!.SetValue(hint, -550f);
            _hintType.GetProperty("YCoordinate")!.SetValue(hint, 160f);
            var alignment = _hintType.GetProperty("Alignment")!;
            alignment.SetValue(hint, Enum.Parse(alignment.PropertyType, "Left"));
            var parameters = _show!.GetParameters();
            var args = parameters.Length == 2 ? new object[] { hint, seconds } :
                new object[] { hint, seconds, Enum.ToObject(parameters[2].ParameterType, 0) };
            _show.Invoke(_getDisplay!.Invoke(null, new object[] { player }), args);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
