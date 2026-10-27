using Godot;

namespace StarTraveler;

/// <summary>One restrained dash for pointer hover and keyboard focus.</summary>
public partial class MenuButton : Button
{
    public string Caption { get; set; } = "";
    private bool _pointerOver;

    public override void _Ready()
    {
        Alignment = HorizontalAlignment.Left;
        MouseEntered += () =>
        {
            _pointerOver = true;
            if (!Disabled) GrabFocus();
            Refresh();
        };
        MouseExited += () => { _pointerOver = false; Refresh(); };
        FocusEntered += Refresh;
        FocusExited += Refresh;
        Refresh();
    }

    public void Refresh() => Text =
        (!Disabled && (HasFocus() || _pointerOver) ? "—   " : "      ") + Caption;
}
