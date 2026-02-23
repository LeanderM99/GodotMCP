#if TOOLS
using Godot;
using Godot.Collections;

namespace GodotMCP.Handlers;

using Math = System.Math;

public class RuntimeHandler : BaseHandler
{
    public RuntimeHandler(EditorPlugin plugin) : base(plugin) { }

    public override Dictionary Handle(string command, Dictionary parms)
    {
        return command switch
        {
            "get_scene_tree" => GetSceneTree(parms),
            "get_node_properties" => GetNodeProperties(parms),
            "capture_frame" => CaptureFrame(),
            "monitor_property" => MonitorProperty(parms),
            _ => Error($"Unknown runtime command: {command}")
        };
    }

    private Dictionary GetSceneTree(Dictionary parms)
    {
        var depth = GetOr(parms, "depth", 10).AsInt32();
        var isPlaying = EditorInterface.Singleton.IsPlayingScene();

        if (!isPlaying)
        {
            return Success(new Dictionary
            {
                { "is_game_running", false },
                { "note", "Game is not running. Use scene.play to start a scene first. The game runs as a separate process, so this tool shows editor autoloads only when no game is active." }
            });
        }

        // Game runs as a child process — the editor tree doesn't contain game nodes.
        // Return a compact summary of the editor tree (autoloads only) and explain the limitation.
        var autoloads = new Array();
        var root = Plugin.GetTree().Root;
        for (int i = 0; i < root.GetChildCount(); i++)
        {
            var child = root.GetChild(i);
            var name = child.Name.ToString();
            // Skip internal editor nodes (they start with @)
            if (name.StartsWith("@")) continue;
            autoloads.Add(new Dictionary { { "name", name }, { "type", child.GetClass() }, { "path", child.GetPath().ToString() } });
        }

        return Success(new Dictionary
        {
            { "is_game_running", true },
            { "autoloads", autoloads },
            { "note", "Godot runs the game as a separate child process. The editor plugin can only see the editor tree (autoloads listed above). Use editor.game_screenshot to capture the game visually, or use the DebuggerEditorPlugin for remote inspection." }
        });
    }

    private Dictionary BuildRuntimeTree(Node node, int maxDepth, int currentDepth)
    {
        var dict = new Dictionary { { "name", node.Name }, { "type", node.GetClass() }, { "path", node.GetPath().ToString() } };
        if (currentDepth < maxDepth && node.GetChildCount() > 0)
        {
            var children = new Array();
            for (int i = 0; i < node.GetChildCount(); i++)
                children.Add(BuildRuntimeTree(node.GetChild(i), maxDepth, currentDepth + 1));
            dict["children"] = children;
        }
        return dict;
    }

    private Dictionary GetNodeProperties(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var node = Plugin.GetTree().Root.GetNodeOrNull(nodePath);
        if (node == null) return Error($"Node not found in runtime tree: {nodePath}");
        var props = new Dictionary();
        foreach (var propDict in node.GetPropertyList())
        {
            var propName = propDict["name"].AsString();
            var usage = propDict["usage"].AsInt32();
            if ((usage & (int)PropertyUsageFlags.Editor) != 0)
            {
                try { props[propName] = node.Get(propName).ToString(); }
                catch { props[propName] = "<unreadable>"; }
            }
        }
        return Success(new Dictionary { { "node_path", nodePath }, { "type", node.GetClass() }, { "properties", props } });
    }

    private Dictionary CaptureFrame()
    {
        var fps = Engine.GetFramesPerSecond();
        var frameTime = 1000.0 / Math.Max(fps, 1);
        return Success(new Dictionary
        {
            { "fps", fps },
            { "frame_time_ms", Math.Round(frameTime, 2) },
            { "object_count", Performance.GetMonitor(Performance.Monitor.ObjectCount) },
            { "node_count", Performance.GetMonitor(Performance.Monitor.ObjectNodeCount) },
            { "orphan_nodes", Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount) },
            { "render_objects_in_frame", Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame) },
            { "render_draw_calls", Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame) },
            { "video_memory_bytes", Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) },
            { "physics_2d_active_objects", Performance.GetMonitor(Performance.Monitor.Physics2DActiveObjects) },
            { "static_memory_bytes", Performance.GetMonitor(Performance.Monitor.MemoryStatic) }
        });
    }

    private Dictionary MonitorProperty(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var property = parms["property"].AsString();
        var node = Plugin.GetTree().Root.GetNodeOrNull(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var value = node.Get(property);
        var samples = new Array();
        samples.Add(new Dictionary { { "timestamp_ms", Time.GetTicksMsec() }, { "value", value.ToString() } });
        return Success(new Dictionary
        {
            { "node_path", nodePath },
            { "property", property },
            { "samples", samples },
            { "note", "Single sample captured. Multi-sample monitoring over duration requires async implementation." }
        });
    }
}
#endif
