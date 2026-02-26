#if TOOLS
using Godot;
using Godot.Collections;
using System.Runtime.InteropServices;

namespace GodotMCP.Handlers;

public class InputHandler : BaseHandler
{
    public InputHandler(EditorPlugin plugin) : base(plugin) { }

    public override Dictionary Handle(string command, Dictionary parms)
    {
        if (!EditorInterface.Singleton.IsPlayingScene())
            return Error("No game is currently running. Start a scene first with scene_play.");
        return command switch
        {
            "key" => SimulateKey(parms),
            "mouse" => SimulateMouse(parms),
            "action" => SimulateAction(parms),
            "text" => SimulateText(parms),
            "sequence" => SimulateSequence(parms),
            _ => Error($"Unknown input command: {command}")
        };
    }

    private System.IntPtr FindAndFocusGameWindow()
    {
        var projectName = ProjectSettings.GetSetting("application/config/name").AsString();
        if (string.IsNullOrEmpty(projectName)) projectName = "Godot";
        var editorPid = (uint)OS.GetProcessId();
        var hwnd = Win32Helper.FindGameWindow(projectName, editorPid);
        if (hwnd != System.IntPtr.Zero)
            Win32Helper.SetForegroundWindow(hwnd);
        return hwnd;
    }

    private static void SendKeyPress(ushort vk, bool pressed)
    {
        var input = new Win32Helper.INPUT
        {
            type = Win32Helper.INPUT_KEYBOARD,
            u = new Win32Helper.InputUnion
            {
                ki = new Win32Helper.KEYBDINPUT
                {
                    wVk = vk,
                    dwFlags = pressed ? 0u : Win32Helper.KEYEVENTF_KEYUP,
                }
            }
        };
        Win32Helper.SendInput(1, new[] { input }, Marshal.SizeOf<Win32Helper.INPUT>());
    }

    private Dictionary SimulateKey(Dictionary parms)
    {
        var keyStr = parms["key"].AsString();
        var pressed = GetOr(parms, "pressed", true).AsBool();
        var vk = Win32Helper.KeyNameToVk(keyStr);
        if (vk == 0) return Error($"Unknown key: {keyStr}");

        var hwnd = FindAndFocusGameWindow();
        if (hwnd == System.IntPtr.Zero)
            return Error("Could not find game window");

        SendKeyPress(vk, pressed);

        if (parms.ContainsKey("duration"))
        {
            var durationMs = parms["duration"].AsInt32();
            var capturedVk = vk;
            var timer = Plugin.GetTree().CreateTimer(durationMs / 1000.0);
            timer.Timeout += () => SendKeyPress(capturedVk, false);
        }

        return Success(new Dictionary { { "key", keyStr }, { "pressed", pressed } });
    }

    private Dictionary SimulateMouse(Dictionary parms)
    {
        var posDict = parms["position"].AsGodotDictionary();
        var x = posDict["x"].AsSingle();
        var y = posDict["y"].AsSingle();
        var button = GetOr(parms, "button", "left").AsString();
        var action = GetOr(parms, "action", "click").AsString();

        var hwnd = FindAndFocusGameWindow();
        if (hwnd == System.IntPtr.Zero)
            return Error("Could not find game window");

        // Get the game window position to convert local coords to screen coords
        Win32Helper.GetClientRect(hwnd, out var rect);
        var windowWidth = rect.Right - rect.Left;
        var windowHeight = rect.Bottom - rect.Top;

        // Convert to absolute screen coordinates (0-65535 range for SendInput)
        // First get screen dimensions
        var screenWidth = DisplayServer.ScreenGetSize().X;
        var screenHeight = DisplayServer.ScreenGetSize().Y;

        // Get window screen position
        var windowPos = new int[2];
        ClientToScreen(hwnd, windowPos);
        var absX = (int)(((windowPos[0] + x) * 65535) / screenWidth);
        var absY = (int)(((windowPos[1] + y) * 65535) / screenHeight);

        var inputSize = Marshal.SizeOf<Win32Helper.INPUT>();

        if (action == "move")
        {
            var moveInput = new Win32Helper.INPUT
            {
                type = Win32Helper.INPUT_MOUSE,
                u = new Win32Helper.InputUnion
                {
                    mi = new Win32Helper.MOUSEINPUT
                    {
                        dx = absX, dy = absY,
                        dwFlags = Win32Helper.MOUSEEVENTF_MOVE | Win32Helper.MOUSEEVENTF_ABSOLUTE,
                    }
                }
            };
            Win32Helper.SendInput(1, new[] { moveInput }, inputSize);
        }
        else
        {
            uint downFlag, upFlag;
            switch (button)
            {
                case "right":
                    downFlag = Win32Helper.MOUSEEVENTF_RIGHTDOWN;
                    upFlag = Win32Helper.MOUSEEVENTF_RIGHTUP;
                    break;
                case "middle":
                    downFlag = Win32Helper.MOUSEEVENTF_MIDDLEDOWN;
                    upFlag = Win32Helper.MOUSEEVENTF_MIDDLEUP;
                    break;
                default:
                    downFlag = Win32Helper.MOUSEEVENTF_LEFTDOWN;
                    upFlag = Win32Helper.MOUSEEVENTF_LEFTUP;
                    break;
            }

            // Move to position first
            var moveInput = new Win32Helper.INPUT
            {
                type = Win32Helper.INPUT_MOUSE,
                u = new Win32Helper.InputUnion
                {
                    mi = new Win32Helper.MOUSEINPUT
                    {
                        dx = absX, dy = absY,
                        dwFlags = Win32Helper.MOUSEEVENTF_MOVE | Win32Helper.MOUSEEVENTF_ABSOLUTE,
                    }
                }
            };
            Win32Helper.SendInput(1, new[] { moveInput }, inputSize);

            if (action == "press" || action == "click")
            {
                var pressInput = new Win32Helper.INPUT
                {
                    type = Win32Helper.INPUT_MOUSE,
                    u = new Win32Helper.InputUnion { mi = new Win32Helper.MOUSEINPUT { dwFlags = downFlag } }
                };
                Win32Helper.SendInput(1, new[] { pressInput }, inputSize);
            }
            if (action == "release" || action == "click")
            {
                var releaseInput = new Win32Helper.INPUT
                {
                    type = Win32Helper.INPUT_MOUSE,
                    u = new Win32Helper.InputUnion { mi = new Win32Helper.MOUSEINPUT { dwFlags = upFlag } }
                };
                Win32Helper.SendInput(1, new[] { releaseInput }, inputSize);
            }
        }

        return Success(new Dictionary { { "action", action }, { "position", $"({x}, {y})" } });
    }

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(System.IntPtr hWnd, int[] lpPoint);

    private Dictionary SimulateAction(Dictionary parms)
    {
        var actionName = parms["action_name"].AsString();
        var pressed = GetOr(parms, "pressed", true).AsBool();

        if (!InputMap.HasAction(actionName))
            return Error($"Unknown input action: {actionName}");

        // Look up the first key event bound to this action
        var events = InputMap.ActionGetEvents(actionName);
        ushort vk = 0;
        foreach (var ev in events)
        {
            if (ev is InputEventKey keyEv)
            {
                // Try to map Godot keycode to VK via the key name
                var keyName = OS.GetKeycodeString(keyEv.Keycode);
                vk = Win32Helper.KeyNameToVk(keyName);
                if (vk != 0) break;
                // Fallback: try physical keycode
                keyName = OS.GetKeycodeString(keyEv.PhysicalKeycode);
                vk = Win32Helper.KeyNameToVk(keyName);
                if (vk != 0) break;
            }
        }

        if (vk == 0)
            return Error($"No keyboard binding found for action '{actionName}'. Only key-bound actions can be simulated.");

        var hwnd = FindAndFocusGameWindow();
        if (hwnd == System.IntPtr.Zero)
            return Error("Could not find game window");

        SendKeyPress(vk, pressed);
        return Success(new Dictionary { { "action", actionName }, { "pressed", pressed }, { "simulated_vk", vk } });
    }

    private Dictionary SimulateText(Dictionary parms)
    {
        var text = parms["text"].AsString();

        var hwnd = FindAndFocusGameWindow();
        if (hwnd == System.IntPtr.Zero)
            return Error("Could not find game window");

        var inputSize = Marshal.SizeOf<Win32Helper.INPUT>();
        foreach (var ch in text)
        {
            var vkScan = Win32Helper.VkKeyScan(ch);
            var vk = (ushort)(vkScan & 0xFF);
            var shift = (vkScan & 0x100) != 0;

            if (shift) SendKeyPress(0x10, true); // VK_SHIFT
            SendKeyPress(vk, true);
            SendKeyPress(vk, false);
            if (shift) SendKeyPress(0x10, false);
        }

        return Success(new Dictionary { { "typed", text }, { "length", text.Length } });
    }

    private Dictionary SimulateSequence(Dictionary parms)
    {
        var steps = parms["steps"].AsGodotArray();
        int stepCount = 0;
        double totalDelay = 0;
        foreach (var step in steps)
        {
            var stepDict = step.AsGodotDictionary();
            var type = stepDict["type"].AsString();
            var stepParams = stepDict.ContainsKey("params") ? stepDict["params"].AsGodotDictionary() : new Dictionary();
            var delayMs = GetOr(stepDict, "delay_ms", 0).AsInt32();
            var currentDelay = totalDelay;
            if (currentDelay > 0)
            {
                var timer = Plugin.GetTree().CreateTimer(currentDelay / 1000.0);
                timer.Timeout += () => ExecuteInputStep(type, stepParams);
            }
            else
            {
                ExecuteInputStep(type, stepParams);
            }
            totalDelay += delayMs;
            stepCount++;
        }
        return Success(new Dictionary { { "steps_queued", stepCount }, { "total_duration_ms", totalDelay } });
    }

    private void ExecuteInputStep(string type, Dictionary parms)
    {
        switch (type)
        {
            case "key": SimulateKey(parms); break;
            case "mouse": SimulateMouse(parms); break;
            case "action": SimulateAction(parms); break;
        }
    }
}
#endif
