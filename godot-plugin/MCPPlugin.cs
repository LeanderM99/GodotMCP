#if TOOLS
using Godot;
using GodotMCP.Handlers;

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
        _router.RegisterHandler("project", new ProjectHandler(this));
        _router.RegisterHandler("scene", new SceneHandler(this));
        _router.RegisterHandler("node", new NodeHandler(this));
        _router.RegisterHandler("script", new ScriptHandler(this));
        _router.RegisterHandler("editor", new EditorHandler(this));
        _router.RegisterHandler("input", new InputHandler(this));
        _router.RegisterHandler("runtime", new RuntimeHandler(this));

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
