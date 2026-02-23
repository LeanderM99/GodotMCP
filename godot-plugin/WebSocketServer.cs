#if TOOLS
using Godot;
using System.Collections.Generic;
using System.Text;

namespace GodotMCP;

[Tool]
public partial class WebSocketServer : Node
{
    private TcpServer _tcpServer = new();
    private readonly Dictionary<int, WebSocketPeer> _peers = new();
    private readonly List<PendingPeer> _pendingPeers = new();
    private int _nextPeerId = 1;
    private int _handshakeTimeoutMs = 3000;

    [Signal] public delegate void ClientConnectedEventHandler(int clientId);
    [Signal] public delegate void ClientDisconnectedEventHandler(int clientId);
    [Signal] public delegate void MessageReceivedEventHandler(int clientId, string message);

    private class PendingPeer
    {
        public StreamPeerTcp Tcp { get; set; }
        public WebSocketPeer Ws { get; set; }
        public int Id { get; set; }
        public ulong ConnectTime { get; set; }
    }

    public Error StartServer(int port)
    {
        var err = _tcpServer.Listen((ushort)port, "127.0.0.1");
        if (err == Error.Ok)
            GD.Print($"[GodotMCP] WebSocket server listening on port {port}");
        else
            GD.PrintErr($"[GodotMCP] Failed to start server on port {port}: {err}");
        return err;
    }

    public void StopServer()
    {
        foreach (var peer in _peers.Values)
            peer.Close();
        _peers.Clear();
        _pendingPeers.Clear();
        _tcpServer.Stop();
        GD.Print("[GodotMCP] WebSocket server stopped");
    }

    public bool IsActive() => _tcpServer.IsListening();

    public void Poll()
    {
        if (!_tcpServer.IsListening())
            return;

        while (_tcpServer.IsConnectionAvailable())
        {
            var tcp = _tcpServer.TakeConnection();
            var id = _nextPeerId++;
            _pendingPeers.Add(new PendingPeer
            {
                Tcp = tcp,
                Id = id,
                ConnectTime = Time.GetTicksMsec()
            });
        }

        var toRemove = new List<PendingPeer>();
        foreach (var p in _pendingPeers)
        {
            if (ProcessPendingPeer(p))
                toRemove.Add(p);
            else if (Time.GetTicksMsec() - p.ConnectTime > (ulong)_handshakeTimeoutMs)
                toRemove.Add(p);
        }
        foreach (var r in toRemove)
            _pendingPeers.Remove(r);

        var disconnected = new List<int>();
        foreach (var (id, peer) in _peers)
        {
            peer.Poll();
            var state = peer.GetReadyState();

            if (state == WebSocketPeer.State.Open)
            {
                while (peer.GetAvailablePacketCount() > 0)
                {
                    var packet = peer.GetPacket();
                    var text = Encoding.UTF8.GetString(packet);
                    EmitSignal(SignalName.MessageReceived, id, text);
                }
            }
            else if (state == WebSocketPeer.State.Closed || state == WebSocketPeer.State.Closing)
            {
                disconnected.Add(id);
            }
        }

        foreach (var id in disconnected)
        {
            _peers.Remove(id);
            EmitSignal(SignalName.ClientDisconnected, id);
        }
    }

    private bool ProcessPendingPeer(PendingPeer p)
    {
        if (p.Ws != null)
        {
            p.Ws.Poll();
            var state = p.Ws.GetReadyState();
            if (state == WebSocketPeer.State.Open)
            {
                _peers[p.Id] = p.Ws;
                EmitSignal(SignalName.ClientConnected, p.Id);
                return true;
            }
            return state != WebSocketPeer.State.Connecting;
        }

        if (p.Tcp.GetStatus() == StreamPeerTcp.Status.Connected)
        {
            p.Ws = new WebSocketPeer();
            p.Ws.AcceptStream(p.Tcp);
            return false;
        }

        return true;
    }

    public Error SendText(int clientId, string text)
    {
        if (!_peers.TryGetValue(clientId, out var peer))
            return Error.DoesNotExist;
        if (peer.GetReadyState() != WebSocketPeer.State.Open)
            return Error.Unavailable;
        return peer.SendText(text);
    }

    public Error Broadcast(string text)
    {
        Error lastErr = Error.Ok;
        foreach (var (_, peer) in _peers)
        {
            if (peer.GetReadyState() == WebSocketPeer.State.Open)
            {
                var err = peer.SendText(text);
                if (err != Error.Ok) lastErr = err;
            }
        }
        return lastErr;
    }
}
#endif
