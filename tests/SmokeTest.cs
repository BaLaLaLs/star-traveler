using Godot;
using System;

namespace StarTraveler;

/// <summary>Run tests/smoke.tscn with -- --test, after building the C# project.</summary>
public partial class SmokeTest : Node
{
    private int _failures;
    private int _checks;
    private void Check(bool value, string label)
    {
        _checks++;
        if (value) GD.Print("PASS: " + label);
        else { GD.PushError("FAIL: " + label); _failures++; }
    }

    public override async void _Ready()
    {
        if (!MainMenu.IsTestMode)
        {
            GD.PushError("Smoke tests require -- --test (protects normal settings and suppresses app exit).");
            GetTree().Quit(2);
            return;
        }
        MainMenu? scene = null;
        try
        {
            // Tests use a separate config, never the player's settings.cfg.
            if (FileAccess.FileExists(MainMenu.TestSettingsPath))
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(MainMenu.TestSettingsPath));
            scene = GD.Load<PackedScene>("res://scenes/main.tscn").Instantiate<MainMenu>();
            AddChild(scene);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Check(scene.MenuButtons.Count == 5, "five real menu buttons");
            Check(scene.MenuButtons[0].Disabled && scene.MenuButtons[2].Disabled, "no-save actions disabled");
            Check(scene.MenuButtons[1].HasFocus(), "initial focus: new adventure");
            Check(scene.MenuButtons[1].Text.StartsWith("—"), "focus dash");
            for (int i = 0; i < 8; i++)
            {
                Press(scene.MenuButtons[3]);
                Check(scene.Screen == "settings" && scene.SettingButtons.Count == 3, $"settings open {i}");
                Press(scene.SettingButtons[2]);
                Check(scene.Screen == "menu" && scene.MenuButtons[3].HasFocus(), $"return focus {i}");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            Press(scene.MenuButtons[3]);
            bool oldMotion = scene.MotionEnabled;
            Press(scene.SettingButtons[1]);
            Check(scene.MotionEnabled != oldMotion, "motion toggle");
            var cfg = new ConfigFile();
            Check(cfg.Load(MainMenu.TestSettingsPath) == Error.Ok &&
                cfg.GetValue("ui", "motion").AsBool() == scene.MotionEnabled, "settings persisted");
            Press(scene.SettingButtons[1]);
            bool oldFullscreen = scene.FullscreenEnabled;
            Press(scene.SettingButtons[0]);
            Check(scene.FullscreenEnabled != oldFullscreen, "fullscreen setting toggle");
            if (DisplayServer.GetName() != "headless")
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var expected = scene.FullscreenEnabled
                    ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed;
                Check(DisplayServer.WindowGetMode() == expected, "actual display mode");
            }
            Press(scene.SettingButtons[0]);
            var cancel = new InputEventAction { Action = "ui_cancel", Pressed = true };
            scene._UnhandledInput(cancel);
            Check(scene.Screen == "menu", "Escape returns from settings");
            Press(scene.MenuButtons[1]);
            Check(scene.Screen == "prototype" && scene.PanelBody.Text.Contains("非可玩关卡"), "honest prototype placeholder");
            scene._UnhandledInput(cancel);
            Check(scene.Screen == "menu", "Escape returns from prototype");
            scene._UnhandledInput(cancel);
            Check(scene.Screen == "exit" && !scene.QuitRequested, "Escape opens exit confirmation");
            Press(scene.SettingButtons[0]);
            Check(scene.Screen == "menu" && !scene.QuitRequested, "exit cancel");
            scene.Notification(Node.NotificationWMCloseRequest);
            Check(scene.Screen == "exit" && !scene.QuitRequested, "window close opens confirmation");
            Press(scene.SettingButtons[0]);
            Press(scene.MenuButtons[4]);
            Press(scene.SettingButtons[1]);
            Check(scene.QuitRequested, "exit confirmed (test suppresses process exit)");
            scene.CloseScreen();
            if (DisplayServer.GetName() != "headless")
            {
                DisplayServer.WindowSetSize(new Vector2I(1024, 768));
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Check(scene.MenuButtons[4].GetGlobalRect().End.Y < scene.Size.Y, "4:3 menu bounds");
            }
        }
        catch (Exception ex)
        {
            GD.PushError(ex.ToString());
            _failures++;
        }
        finally
        {
            if (GodotObject.IsInstanceValid(scene)) scene!.QueueFree();
            if (FileAccess.FileExists(MainMenu.TestSettingsPath))
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(MainMenu.TestSettingsPath));
            GD.Print($"C# SMOKE RESULT: {_checks} check(s), {_failures} failure(s)");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
    }

    private static void Press(BaseButton button) => button.EmitSignal(BaseButton.SignalName.Pressed);
}
