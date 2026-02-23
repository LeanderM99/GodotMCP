#if TOOLS
using Godot;

namespace GodotMCP;

[Tool]
public partial class MCPPlugin : EditorPlugin
{
    private const int DefaultPort = 6550;

    public override void _EnterTree()
    {
        GD.Print("[GodotMCP] Plugin enabled");
        Engine.SetMeta("MCPPlugin", this);
    }

    public override void _ExitTree()
    {
        Engine.RemoveMeta("MCPPlugin");
        GD.Print("[GodotMCP] Plugin disabled");
    }

    public override void _Process(double delta)
    {
        // Will poll WebSocket server here
    }
}
#endif
