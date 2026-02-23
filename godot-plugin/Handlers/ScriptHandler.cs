#if TOOLS
using Godot;
using Godot.Collections;

namespace GodotMCP.Handlers;

public class ScriptHandler : BaseHandler
{
    public ScriptHandler(EditorPlugin plugin) : base(plugin) { }

    public override Dictionary Handle(string command, Dictionary parms)
    {
        return command switch
        {
            "list" => ListScripts(parms),
            "read" => ReadScript(parms),
            "create" => CreateScript(parms),
            "edit" => EditScript(parms),
            "attach" => AttachScript(parms),
            "detach" => DetachScript(parms),
            _ => Error($"Unknown script command: {command}")
        };
    }

    private Dictionary ListScripts(Dictionary parms)
    {
        var path = GetOr(parms,"path", "res://").AsString();
        var language = GetOr(parms,"language", "all").AsString();
        var scripts = new Array();
        CollectScripts(path, language, scripts);
        return Success(new Dictionary { { "scripts", scripts }, { "count", scripts.Count } });
    }

    private void CollectScripts(string path, string language, Array scripts)
    {
        var dir = DirAccess.Open(path);
        if (dir == null) return;
        dir.ListDirBegin();
        var fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            var fullPath = path.TrimEnd('/') + "/" + fileName;
            if (dir.CurrentIsDir())
            {
                if (!fileName.StartsWith(".") && fileName != "addons")
                    CollectScripts(fullPath, language, scripts);
            }
            else
            {
                bool include = language switch
                {
                    "cs" => fileName.EndsWith(".cs"),
                    "gd" => fileName.EndsWith(".gd"),
                    _ => fileName.EndsWith(".cs") || fileName.EndsWith(".gd")
                };
                if (include) scripts.Add(fullPath);
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();
    }

    private Dictionary ReadScript(Dictionary parms)
    {
        var path = parms["path"].AsString();
        if (!FileAccess.FileExists(path)) return Error($"Script not found: {path}");
        var content = FileAccess.GetFileAsString(path);
        return Success(new Dictionary { { "path", path }, { "content", content } });
    }

    private Dictionary CreateScript(Dictionary parms)
    {
        var path = parms["path"].AsString();
        var content = parms["content"].AsString();
        if (FileAccess.FileExists(path)) return Error($"Script already exists: {path}");
        var dir = path[..path.LastIndexOf('/')];
        DirAccess.MakeDirRecursiveAbsolute(dir);
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null) return Error($"Cannot create: {path}");
        file.StoreString(content);
        file.Close();
        EditorInterface.Singleton.GetResourceFilesystem().Scan();
        return Success(new Dictionary { { "path", path } });
    }

    private Dictionary EditScript(Dictionary parms)
    {
        var path = parms["path"].AsString();
        var content = parms["content"].AsString();
        if (!FileAccess.FileExists(path)) return Error($"Script not found: {path}");
        var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null) return Error($"Cannot write to: {path}");
        file.StoreString(content);
        file.Close();
        EditorInterface.Singleton.GetResourceFilesystem().Scan();
        return Success(new Dictionary { { "path", path } });
    }

    private Dictionary AttachScript(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var scriptPath = parms["script_path"].AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        var script = ResourceLoader.Load<Script>(scriptPath);
        if (script == null) return Error($"Script not found: {scriptPath}");
        node.SetScript(script);
        return Success(new Dictionary { { "node_path", nodePath }, { "script_path", scriptPath } });
    }

    private Dictionary DetachScript(Dictionary parms)
    {
        var nodePath = parms["node_path"].AsString();
        var node = FindNode(nodePath);
        if (node == null) return Error($"Node not found: {nodePath}");
        node.SetScript(default(Variant));
        return Success(new Dictionary { { "node_path", nodePath } });
    }
}
#endif
