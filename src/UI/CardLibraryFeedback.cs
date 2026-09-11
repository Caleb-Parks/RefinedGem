using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;

namespace RefinedGem.UI;

/// <summary>
/// Transient status text shown on the Card Library while editing the refined pool.
/// </summary>
internal static class CardLibraryFeedback
{
    private const string NodeName = "RefinedGemFeedbackLabel";
    private const double VisibleSeconds = 1.6;
    private const double FadeSeconds = 0.35;

    private static NCardLibrary? _library;
    private static Label? _label;
    private static Tween? _tween;

    public static void Bind(NCardLibrary library)
    {
        if (_library == library && _label is not null && GodotObject.IsInstanceValid(_label))
            return;

        Detach();
        _library = library;
    }

    public static void Show(string message)
    {
        if (_library is null || !GodotObject.IsInstanceValid(_library))
            return;

        EnsureLabel();
        if (_label is null || !GodotObject.IsInstanceValid(_label))
            return;

        _label.Text = message;
        _label.Modulate = Colors.White;
        _label.Visible = true;

        _tween?.Kill();
        _tween = _label.CreateTween();
        _tween.TweenInterval(VisibleSeconds);
        _tween.TweenProperty(_label, "modulate:a", 0f, FadeSeconds);
        _tween.TweenCallback(Callable.From(() =>
        {
            if (_label is not null && GodotObject.IsInstanceValid(_label))
                _label.Visible = false;
        }));
    }

    public static void Detach()
    {
        _tween?.Kill();
        _tween = null;

        if (_label is not null && GodotObject.IsInstanceValid(_label))
            _label.QueueFree();

        _label = null;
        _library = null;
    }

    private static void EnsureLabel()
    {
        if (_library is null || !GodotObject.IsInstanceValid(_library))
            return;

        if (_label is not null && GodotObject.IsInstanceValid(_label))
            return;

        _label = _library.GetNodeOrNull<Label>(NodeName);
        if (_label is not null)
            return;

        _label = new Label
        {
            Name = NodeName,
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _label.AddThemeFontSizeOverride("font_size", 18);
        _label.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _label.OffsetLeft = -220;
        _label.OffsetRight = 220;
        _label.OffsetTop = 72;
        _label.OffsetBottom = 112;
        _library.AddChild(_label);
    }
}
