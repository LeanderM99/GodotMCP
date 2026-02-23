#if TOOLS
using Godot;
using Godot.Collections;

namespace GodotMCP.Handlers;

public class NodeHandler : BaseHandler
{
    public NodeHandler(EditorPlugin plugin) : base(plugin) { }

    public override Dictionary Handle(string command, Dictionary parms)
    {
        return command switch
        {
            "add" => AddNode(parms),
            "delete" => DeleteNode(parms),
            "rename" => RenameNode(parms),
            "duplicate" => DuplicateNode(parms),
            "move" => MoveNode(parms),
            "get_properties" => GetProperties(parms),
            "set_property" => SetProperty(parms),
            "get_signals" => GetSignals(parms),
            "connect_signal" => ConnectSignal(parms),
            "disconnect_signal" => DisconnectSignal(parms),
            "get_children" => GetChildren(parms),
            _ => Error($"Unknown node command: {command}")
        };
    }

    private Dictionary AddNode(Dictionary parms)
    {
        var parentPath = parms["parent_path"].AsString();
        var type = parms["type"].AsString();
        var name = parms["name"].AsString();
        var parent = FindNode(parentPath);
        if (parent == null) return Error($"Parent not found: {parentPath}");
        var node = ClassDB.Instantiate(type).AsGodotObject() as Node;
        if (node == null) return Error($"Invalid node type: {type}");
        node.Name = name;
        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction("Add Node");
        undoRedo.AddDoMethod(parent, "add_child", node);
        undoRedo.AddDoProperty(node, "owner", GetEditedRoot());
        undoRedo.AddDoReference(node);
        undoRedo.AddUndoMethod(parent, "remove_child", node);
        undoRedo.CommitAction();
        return Success(new Dictionary { { "node_path", node.GetPath().ToString() }, { "name", node.Name } });
    }

    private Dictionary DeleteNode(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        if (node == GetEditedRoot()) return Error("Cannot delete scene root node");
        var parent = node.GetParent();
        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction("Delete Node");
        undoRedo.AddDoMethod(parent, "remove_child", node);
        undoRedo.AddUndoMethod(parent, "add_child", node);
        undoRedo.AddUndoProperty(node, "owner", GetEditedRoot());
        undoRedo.AddUndoReference(node);
        undoRedo.CommitAction();
        return Success(new Dictionary { { "deleted", nodePath } });
    }

    private Dictionary RenameNode(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var newName = parms["new_name"].AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var oldName = node.Name;
        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction("Rename Node");
        undoRedo.AddDoProperty(node, "name", newName);
        undoRedo.AddUndoProperty(node, "name", oldName);
        undoRedo.CommitAction();
        return Success(new Dictionary { { "old_name", oldName }, { "new_name", newName } });
    }

    private Dictionary DuplicateNode(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var newName = parms.GetValueOrDefault("new_name", "").AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var duplicate = node.Duplicate();
        if (!string.IsNullOrEmpty(newName)) duplicate.Name = newName;
        var parent = node.GetParent();
        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction("Duplicate Node");
        undoRedo.AddDoMethod(parent, "add_child", duplicate);
        undoRedo.AddDoProperty(duplicate, "owner", GetEditedRoot());
        undoRedo.AddDoReference(duplicate);
        undoRedo.AddUndoMethod(parent, "remove_child", duplicate);
        undoRedo.CommitAction();
        return Success(new Dictionary { { "node_path", duplicate.GetPath().ToString() }, { "name", duplicate.Name } });
    }

    private Dictionary MoveNode(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var newParentPath = parms["new_parent_path"].AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var newParent = FindNode(newParentPath);
        if (newParent == null) return Error($"New parent not found: {newParentPath}");
        var oldParent = node.GetParent();
        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction("Move Node");
        undoRedo.AddDoMethod(oldParent, "remove_child", node);
        undoRedo.AddDoMethod(newParent, "add_child", node);
        undoRedo.AddDoProperty(node, "owner", GetEditedRoot());
        undoRedo.AddUndoMethod(newParent, "remove_child", node);
        undoRedo.AddUndoMethod(oldParent, "add_child", node);
        undoRedo.AddUndoProperty(node, "owner", GetEditedRoot());
        undoRedo.CommitAction();
        return Success(new Dictionary { { "node_path", node.GetPath().ToString() } });
    }

    private Dictionary GetProperties(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var props = new Dictionary();
        foreach (var prop in node.GetPropertyList())
        {
            var propDict = prop.AsGodotDictionary();
            var propName = propDict["name"].AsString();
            var usage = propDict["usage"].AsInt32();
            if ((usage & (int)PropertyUsageFlags.Editor) != 0)
                props[propName] = node.Get(propName).ToString();
        }
        return Success(new Dictionary { { "properties", props }, { "type", node.GetClass() } });
    }

    private Dictionary SetProperty(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var property = parms["property"].AsString();
        var value = parms["value"];
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var oldValue = node.Get(property);
        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction("Set Property");
        undoRedo.AddDoProperty(node, property, value);
        undoRedo.AddUndoProperty(node, property, oldValue);
        undoRedo.CommitAction();
        return Success(new Dictionary { { "property", property }, { "value", node.Get(property).ToString() } });
    }

    private Dictionary GetSignals(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var signals = new Array();
        foreach (var sig in node.GetSignalList())
        {
            var sigDict = sig.AsGodotDictionary();
            signals.Add(new Dictionary { { "name", sigDict["name"] }, { "args", sigDict.GetValueOrDefault("args", new Array()) } });
        }
        return Success(new Dictionary { { "signals", signals } });
    }

    private Dictionary ConnectSignal(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var signalName = parms["signal"].AsString();
        var targetPath = parms["target_path"].AsString();
        var method = parms["method"].AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var target = FindNode(targetPath);
        if (target == null) return Error($"Target not found: {targetPath}");
        var err = node.Connect(signalName, new Callable(target, method));
        if (err != Error.Ok) return Error($"Failed to connect signal: {err}");
        return Success(new Dictionary { { "connected", $"{signalName} -> {targetPath}.{method}" } });
    }

    private Dictionary DisconnectSignal(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var signalName = parms["signal"].AsString();
        var targetPath = parms["target_path"].AsString();
        var method = parms["method"].AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var target = FindNode(targetPath);
        if (target == null) return Error($"Target not found: {targetPath}");
        if (!node.IsConnected(signalName, new Callable(target, method)))
            return Error("Signal connection not found");
        node.Disconnect(signalName, new Callable(target, method));
        return Success(new Dictionary { { "disconnected", $"{signalName} -> {targetPath}.{method}" } });
    }

    private Dictionary GetChildren(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var children = new Array();
        for (int i = 0; i < node.GetChildCount(); i++)
        {
            var child = node.GetChild(i);
            children.Add(new Dictionary { { "name", child.Name }, { "type", child.GetClass() }, { "index", i } });
        }
        return Success(new Dictionary { { "children", children }, { "count", node.GetChildCount() } });
    }
}
#endif
