#if TOOLS
using Godot;
using Godot.Collections;

namespace GodotMCP.Handlers;

public abstract class BaseHandler
{
    protected EditorPlugin Plugin { get; }

    protected BaseHandler(EditorPlugin plugin)
    {
        Plugin = plugin;
    }

    public abstract Dictionary Handle(string command, Dictionary parms);

    protected Node GetEditedRoot() =>
        EditorInterface.Singleton.GetEditedSceneRoot();

    protected Node FindNode(string path)
    {
        var root = GetEditedRoot();
        if (root == null) return null;
        if (path == "." || path == root.Name) return root;
        if (path.StartsWith("/root/"))
            path = path["/root/".Length..];
        var rootName = root.Name + "/";
        if (path.StartsWith(rootName))
            path = path[rootName.Length..];
        else if (path == root.Name)
            return root;
        return root.GetNodeOrNull(path);
    }

    protected EditorUndoRedoManager GetUndoRedo() =>
        Plugin.GetUndoRedo();

    protected Dictionary Success(Dictionary data = null) =>
        new() { { "success", true }, { "data", data ?? new Dictionary() } };

    protected Dictionary Error(string message) =>
        new() { { "success", false }, { "error", message } };
}
#endif
