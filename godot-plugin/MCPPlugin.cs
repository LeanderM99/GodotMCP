#if TOOLS
using Godot;

namespace GodotMCP;

[Tool]
public partial class MCPPlugin : EditorPlugin
{
    private const int DefaultPort = 6550;
    private WebSocketServer _wsServer;
    private CommandRouter _router;

    public override void _EnterTree()
    {
        Engine.SetMeta("MCPPlugin", this);

        _wsServer = new WebSocketServer();
        AddChild(_wsServer);
        _wsServer.StartServer(DefaultPort);

        _wsServer.ClientConnected += OnClientConnected;
        _wsServer.ClientDisconnected += OnClientDisconnected;
        _wsServer.MessageReceived += OnMessageReceived;

        _router = new CommandRouter();
        // Handlers registered in subsequent tasks

        GD.Print("[GodotMCP] Plugin enabled");
    }

    public override void _ExitTree()
    {
        _wsServer?.StopServer();
        _wsServer?.QueueFree();
        Engine.RemoveMeta("MCPPlugin");
        GD.Print("[GodotMCP] Plugin disabled");
    }

    public override void _Process(double delta)
    {
        _wsServer?.Poll();
    }

    private void OnClientConnected(int clientId)
    {
        GD.Print($"[GodotMCP] Client {clientId} connected");
    }

    private void OnClientDisconnected(int clientId)
    {
        GD.Print($"[GodotMCP] Client {clientId} disconnected");
    }

    private void OnMessageReceived(int clientId, string message)
    {
        var response = _router.Route(message);
        _wsServer.SendText(clientId, response);
    }
}
#endif
