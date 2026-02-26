#if TOOLS
using Godot;
using Godot.Collections;

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

    private Dictionary SimulateKey(Dictionary parms)
    {
        var keyStr = parms["key"].AsString();
        var pressed = GetOr(parms,"pressed", true).AsBool();
        var keyEvent = new InputEventKey();
        keyEvent.Keycode = OS.FindKeycodeFromString(keyStr);
        keyEvent.Pressed = pressed;
        keyEvent.PhysicalKeycode = keyEvent.Keycode;
        Input.ParseInputEvent(keyEvent);
        if (parms.ContainsKey("duration"))
        {
            var durationMs = parms["duration"].AsInt32();
            var timer = Plugin.GetTree().CreateTimer(durationMs / 1000.0);
            timer.Timeout += () =>
            {
                var releaseEvent = new InputEventKey();
                releaseEvent.Keycode = keyEvent.Keycode;
                releaseEvent.Pressed = false;
                releaseEvent.PhysicalKeycode = keyEvent.Keycode;
                Input.ParseInputEvent(releaseEvent);
            };
        }
        return Success(new Dictionary { { "key", keyStr }, { "pressed", pressed } });
    }

    private Dictionary SimulateMouse(Dictionary parms)
    {
        var posDict = parms["position"].AsGodotDictionary();
        var x = posDict["x"].AsSingle();
        var y = posDict["y"].AsSingle();
        var button = GetOr(parms,"button", "left").AsString();
        var action = GetOr(parms,"action", "click").AsString();
        var buttonIndex = button switch { "right" => MouseButton.Right, "middle" => MouseButton.Middle, _ => MouseButton.Left };
        if (action == "move")
        {
            var moveEvent = new InputEventMouseMotion();
            moveEvent.Position = new Vector2(x, y);
            moveEvent.GlobalPosition = new Vector2(x, y);
            Input.ParseInputEvent(moveEvent);
        }
        else
        {
            var clickEvent = new InputEventMouseButton();
            clickEvent.Position = new Vector2(x, y);
            clickEvent.GlobalPosition = new Vector2(x, y);
            clickEvent.ButtonIndex = buttonIndex;
            clickEvent.Pressed = action != "release";
            Input.ParseInputEvent(clickEvent);
            if (action == "click")
            {
                var releaseEvent = new InputEventMouseButton();
                releaseEvent.Position = new Vector2(x, y);
                releaseEvent.GlobalPosition = new Vector2(x, y);
                releaseEvent.ButtonIndex = buttonIndex;
                releaseEvent.Pressed = false;
                Input.ParseInputEvent(releaseEvent);
            }
        }
        return Success(new Dictionary { { "action", action }, { "position", $"({x}, {y})" } });
    }

    private Dictionary SimulateAction(Dictionary parms)
    {
        var actionName = parms["action_name"].AsString();
        var pressed = GetOr(parms,"pressed", true).AsBool();
        var strength = GetOr(parms,"strength", 1.0f).AsSingle();
        if (!InputMap.HasAction(actionName)) return Error($"Unknown input action: {actionName}");
        var actionEvent = new InputEventAction();
        actionEvent.Action = actionName;
        actionEvent.Pressed = pressed;
        actionEvent.Strength = strength;
        Input.ParseInputEvent(actionEvent);
        return Success(new Dictionary { { "action", actionName }, { "pressed", pressed }, { "strength", strength } });
    }

    private Dictionary SimulateText(Dictionary parms)
    {
        var text = parms["text"].AsString();
        foreach (var ch in text)
        {
            var keyEvent = new InputEventKey();
            keyEvent.Unicode = ch;
            keyEvent.Pressed = true;
            Input.ParseInputEvent(keyEvent);
            var releaseEvent = new InputEventKey();
            releaseEvent.Unicode = ch;
            releaseEvent.Pressed = false;
            Input.ParseInputEvent(releaseEvent);
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
