using Godot;
using System;
using System.Collections.Generic;

namespace StarTraveler;

/// <summary>Real Controls over a text-free illustration; no gameplay/save system.</summary>
public partial class MainMenu : Control
{
    public const string SettingsPath = "user://settings.cfg";
    public const string TestSettingsPath = "user://settings.smoke.cfg";
    private static readonly Color Ink = new("f4ead8");
    public bool MotionEnabled { get; private set; } = true;
    public bool FullscreenEnabled { get; private set; }
    public string Screen { get; private set; } = "menu";
    public bool QuitRequested { get; private set; }
    public List<MenuButton> MenuButtons { get; } = new();
    public List<MenuButton> SettingButtons { get; } = new();
    public Label PanelBody { get; private set; } = null!;
    private Control _menu = null!;
    private Control _panel = null!;
    private Label _titleLabel = null!;
    private Label _panelTitle = null!;
    private MenuButton? _returnFocus;
    private Tween? _fade;
    public static bool IsTestMode => Array.IndexOf(OS.GetCmdlineUserArgs(), "--test") >= 0;
    public string ActiveSettingsPath => IsTestMode ? TestSettingsPath : SettingsPath;

    public override void _Ready()
    {
        LoadSettings();
        GetTree().AutoAcceptQuit = false;
        BuildTheme();
        BuildBackground();
        BuildMenu();
        BuildPanel();
        Resized += Layout;
        Layout();
        MenuButtons[1].GrabFocus();
        if (MotionEnabled && !IsTestMode)
        {
            _menu.Modulate = new Color(1, 1, 1, 0);
            _fade = CreateTween();
            _fade.TweenProperty(_menu, "modulate:a", 1.0f, 0.75);
        }
    }

    private void BuildTheme()
    {
        var uiTheme = new Theme
        {
            DefaultFont = GD.Load<Font>("res://assets/JourneySerifSC-Subset.otf"),
            DefaultFontSize = 26
        };
        foreach (string state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
            uiTheme.SetStylebox(state, "Button", new StyleBoxEmpty());
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
            uiTheme.SetColor(state, "Button", Ink);
        uiTheme.SetColor("font_disabled_color", "Button", new Color(0.83f, 0.82f, 0.79f, 0.55f));
        uiTheme.SetColor("font_color", "Label", Ink);
        Theme = uiTheme;
    }

    private void BuildBackground()
    {
        var background = new TextureRect
        {
            Name = "Landscape",
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore,
            Texture = GD.Load<Texture2D>("res://assets/background.png")
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(0.06f, 0.10f, 0.17f, 0.12f));
        gradient.SetColor(1, new Color(0.06f, 0.10f, 0.17f, 0));
        var veil = new TextureRect
        {
            Texture = new GradientTexture2D
            {
                Gradient = gradient, FillFrom = Vector2.Zero, FillTo = new Vector2(0.65f, 0)
            },
            MouseFilter = MouseFilterEnum.Ignore
        };
        veil.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(veil);
    }

    private void BuildMenu()
    {
        _menu = new Control { Name = "Menu", MouseFilter = MouseFilterEnum.Ignore };
        _menu.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_menu);
        _titleLabel = MakeLabel(_menu, "星 旅 物 语", 72);
        _titleLabel.Name = "Title";
        string[] captions = { "继续旅程", "新的冒险", "载入存档", "设置", "退出游戏" };
        string[] names = { "Continue", "NewAdventure", "Load", "Settings", "Exit" };
        for (int i = 0; i < captions.Length; i++)
        {
            var button = MakeButton(_menu, captions[i]);
            button.Name = names[i];
            MenuButtons.Add(button);
            if (i == 0 || i == 2)
            {
                button.Disabled = true;
                button.TooltipText = "暂无存档。本原型尚未实现游戏与存档系统。";
                button.Refresh();
            }
            else
            {
                int index = i; // Capture each button's index, not the loop variable.
                button.Pressed += () => MenuAction(index);
            }
        }
    }

    private void BuildPanel()
    {
        _panel = new Control { Name = "Subscreen", MouseFilter = MouseFilterEnum.Ignore };
        _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_panel);
        _panelTitle = MakeLabel(_panel, "", 52);
        PanelBody = MakeLabel(_panel, "", 24);
        PanelBody.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _panel.Hide();
    }

    private static Label MakeLabel(Node parent, string text, int fontSize)
    {
        var label = new Label { Text = text, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        parent.AddChild(label);
        return label;
    }

    private static MenuButton MakeButton(Node parent, string caption)
    {
        var button = new MenuButton { Caption = caption, CustomMinimumSize = new Vector2(360, 54) };
        parent.AddChild(button);
        return button;
    }

    private void MenuAction(int index)
    {
        _returnFocus = MenuButtons[index];
        switch (index)
        {
            case 1: OpenScreen("prototype"); break;
            case 3: OpenScreen("settings"); break;
            case 4: OpenScreen("exit"); break;
        }
    }

    public void OpenScreen(string nextScreen)
    {
        if (nextScreen != "settings" && nextScreen != "prototype" && nextScreen != "exit")
            throw new ArgumentException("Unknown menu screen", nameof(nextScreen));
        // Detach immediately; queue deletion safely after the current signal callback.
        foreach (var button in SettingButtons)
        {
            _panel.RemoveChild(button);
            button.QueueFree();
        }
        SettingButtons.Clear();
        Screen = nextScreen;
        _menu.Hide();
        _panel.Show();
        switch (Screen)
        {
            case "settings":
                _panelTitle.Text = "设置";
                PanelBody.Text = "改动自动保存。";
                AddPanelButton(DisplayCaption(), ToggleFullscreen);
                AddPanelButton(MotionCaption(), ToggleMotion);
                AddPanelButton("返回", CloseScreen);
                break;
            case "prototype":
                _panelTitle.Text = "新的冒险";
                PanelBody.Text = "主菜单原型 · 非可玩关卡\n\n旅程尚未开始。这里预留给未来的游戏场景。\n当前仅演示菜单交互，不创建角色或存档。";
                AddPanelButton("返回主菜单", CloseScreen);
                break;
            case "exit":
                _panelTitle.Text = "离开旅途？";
                PanelBody.Text = "确定退出主菜单原型吗？";
                AddPanelButton("取消", CloseScreen);
                AddPanelButton("退出游戏", ConfirmExit);
                break;
        }
        Layout();
        SettingButtons[0].GrabFocus();
    }

    private void AddPanelButton(string caption, Action action)
    {
        var button = MakeButton(_panel, caption);
        button.Pressed += action;
        SettingButtons.Add(button);
    }

    public void CloseScreen()
    {
        Screen = "menu";
        _panel.Hide();
        _menu.Show();
        if (GodotObject.IsInstanceValid(_returnFocus)) _returnFocus!.GrabFocus();
        else MenuButtons[1].GrabFocus();
    }

    private void ToggleFullscreen()
    {
        FullscreenEnabled = !FullscreenEnabled;
        ApplyDisplay();
        SaveSettings();
        SettingButtons[0].Caption = DisplayCaption();
        SettingButtons[0].Refresh();
    }

    private void ToggleMotion()
    {
        MotionEnabled = !MotionEnabled;
        if (!MotionEnabled && GodotObject.IsInstanceValid(_fade))
        {
            _fade!.Kill();
            _menu.Modulate = Colors.White;
        }
        SaveSettings();
        SettingButtons[1].Caption = MotionCaption();
        SettingButtons[1].Refresh();
    }

    private string DisplayCaption() => "显示模式：" + (FullscreenEnabled ? "全屏" : "窗口");
    private string MotionCaption() => "轻柔淡入：" + (MotionEnabled ? "开启" : "关闭");

    private void ApplyDisplay()
    {
        if (DisplayServer.GetName() != "headless")
            DisplayServer.WindowSetMode(FullscreenEnabled
                ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
    }

    private void LoadSettings()
    {
        var config = new ConfigFile();
        if (config.Load(ActiveSettingsPath) == Error.Ok)
        {
            MotionEnabled = config.GetValue("ui", "motion", true).AsBool();
            FullscreenEnabled = config.GetValue("ui", "fullscreen", false).AsBool();
        }
        ApplyDisplay();
    }

    private void SaveSettings()
    {
        var config = new ConfigFile();
        config.SetValue("ui", "motion", MotionEnabled);
        config.SetValue("ui", "fullscreen", FullscreenEnabled);
        var result = config.Save(ActiveSettingsPath);
        if (result != Error.Ok) PanelBody.Text = $"设置暂时无法保存（错误 {result}）。";
    }

    private void Layout()
    {
        if (!GodotObject.IsInstanceValid(_titleLabel)) return;
        float left = Size.X * 0.092f;
        float top = Size.Y * 0.205f;
        _titleLabel.Position = new Vector2(left, top);
        _titleLabel.Size = new Vector2(620, 115);
        for (int i = 0; i < MenuButtons.Count; i++)
        {
            MenuButtons[i].Position = new Vector2(left - 40, top + 139 + i * 55);
            MenuButtons[i].Size = new Vector2(390, 54);
        }
        _panelTitle.Position = new Vector2(left, top);
        _panelTitle.Size = new Vector2(Size.X * 0.8f, 90);
        PanelBody.Position = new Vector2(left + 3, top + 108);
        PanelBody.Size = new Vector2(Mathf.Min(720, Size.X * 0.73f), 180);
        for (int i = 0; i < SettingButtons.Count; i++)
        {
            SettingButtons[i].Position = new Vector2(left - 40,
                top + (Screen == "prototype" ? 305 : 215) + i * 60);
            SettingButtons[i].Size = new Vector2(610, 55);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("ui_cancel")) return;
        if (Screen == "menu")
        {
            _returnFocus = MenuButtons[4];
            OpenScreen("exit");
        }
        else CloseScreen();
        GetViewport().SetInputAsHandled();
    }

    public override void _Notification(int what)
    {
        if (what != NotificationWMCloseRequest || MenuButtons.Count < 5) return;
        _returnFocus = MenuButtons[4];
        OpenScreen("exit");
    }

    private void ConfirmExit()
    {
        QuitRequested = true;
        if (!IsTestMode) GetTree().Quit();
    }
}
