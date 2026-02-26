#if TOOLS
using Godot;
using Godot.Collections;

namespace GodotMCP.Handlers;

public class SceneHandler : BaseHandler
{
    public SceneHandler(EditorPlugin plugin) : base(plugin) { }

    public override Dictionary Handle(string command, Dictionary parms)
    {
        return command switch
        {
            "create" => CreateScene(parms),
            "open" => OpenScene(parms),
            "get_current" => GetCurrentScene(),
            "save" => SaveScene(parms),
            "delete" => DeleteScene(parms),
            "instance" => InstanceScene(parms),
            "get_tree" => GetTree(parms),
            "play" => PlayScene(parms),
            "stop" => StopScene(),
            _ => Error($"Unknown scene command: {command}")
        };
    }

    private Dictionary CreateScene(Dictionary parms)
    {
        var path = parms["path"].AsString();
        var rootType = parms["root_type"].AsString();
        var rootName = GetOr(parms,"root_name", "").AsString();

        var node = ClassDB.Instantiate(rootType).AsGodotObject() as Node;
        if (node == null)
            return Error($"Invalid node type: {rootType}");

        node.Name = !string.IsNullOrEmpty(rootName) ? rootName : path.GetFile().GetBaseName();

        var scene = new PackedScene();
        scene.Pack(node);
        var err = ResourceSaver.Save(scene, path);
        node.Free();

        if (err != Godot.Error.Ok)
            return Error($"Failed to save scene: {err}");

        EditorInterface.Singleton.GetResourceFilesystem().Scan();
        return Success(new Dictionary { { "path", path } });
    }

    private Dictionary OpenScene(Dictionary parms)
    {
        var path = parms["path"].AsString();
        EditorInterface.Singleton.OpenSceneFromPath(path);
        // Force the main screen to 2D so the viewport actually renders the opened scene
        EditorInterface.Singleton.SetMainScreenEditor("2D");
        return Success(new Dictionary { { "path", path } });
    }

    private Dictionary GetCurrentScene()
    {
        var root = GetEditedRoot();
        if (root == null) return Error("No scene is currently open");
        return Success(new Dictionary { { "name", root.Name }, { "path", root.SceneFilePath }, { "type", root.GetClass() } });
    }

    private Dictionary SaveScene(Dictionary parms)
    {
        var path = GetOr(parms,"path", "").AsString();
        if (string.IsNullOrEmpty(path))
        {
            EditorInterface.Singleton.SaveScene();
            return Success(new Dictionary { { "saved", "current" } });
        }
        EditorInterface.Singleton.OpenSceneFromPath(path);
        EditorInterface.Singleton.SaveScene();
        return Success(new Dictionary { { "path", path } });
    }

    private Dictionary DeleteScene(Dictionary parms)
    {
        var path = parms["path"].AsString();
        if (!FileAccess.FileExists(path)) return Error($"Scene not found: {path}");
        var err = DirAccess.RemoveAbsolute(path);
        if (err != Godot.Error.Ok) return Error($"Failed to delete: {err}");
        if (FileAccess.FileExists(path + ".import"))
            DirAccess.RemoveAbsolute(path + ".import");
        EditorInterface.Singleton.GetResourceFilesystem().Scan();
        return Success(new Dictionary { { "deleted", path } });
    }

    private Dictionary InstanceScene(Dictionary parms)
    {
        var scenePath = parms["scene_path"].AsString();
        var parentPath = parms["parent_path"].AsString();
        var name = GetOr(parms,"name", "").AsString();

        var parent = FindNode(parentPath);
        if (parent == null) return Error($"Parent node not found: {parentPath}");

        var packedScene = ResourceLoader.Load<PackedScene>(scenePath);
        if (packedScene == null) return Error($"Cannot load scene: {scenePath}");

        var instance = packedScene.Instantiate();
        if (!string.IsNullOrEmpty(name)) instance.Name = name;

        var undoRedo = GetUndoRedo();
        undoRedo.CreateAction("Instance Scene");
        undoRedo.AddDoMethod(parent, "add_child", instance);
        undoRedo.AddDoProperty(instance, "owner", GetEditedRoot());
        undoRedo.AddDoReference(instance);
        undoRedo.AddUndoMethod(parent, "remove_child", instance);
        undoRedo.CommitAction();

        return Success(new Dictionary { { "node_path", instance.GetPath().ToString() }, { "name", instance.Name } });
    }

    private Dictionary GetTree(Dictionary parms)
    {
        var path = GetOr(parms, "path", "").AsString();
        if (!string.IsNullOrEmpty(path))
        {
            // Load and inspect the requested scene without opening it in the editor.
            var packed = ResourceLoader.Load<PackedScene>(path);
            if (packed == null) return Error($"Cannot load scene: {path}");
            var tempRoot = packed.Instantiate();
            var tree = BuildTreeDict(tempRoot);
            tempRoot.QueueFree();
            return Success(new Dictionary { { "tree", tree }, { "path", path } });
        }
        var root = GetEditedRoot();
        if (root == null) return Error("No scene is currently open");
        return Success(new Dictionary { { "tree", BuildTreeDict(root) } });
    }

    private Dictionary BuildTreeDict(Node node)
    {
        var dict = new Dictionary { { "name", node.Name }, { "type", node.GetClass() } };
        if (node.GetChildCount() > 0)
        {
            var children = new Array();
            for (int i = 0; i < node.GetChildCount(); i++)
                children.Add(BuildTreeDict(node.GetChild(i)));
            dict["children"] = children;
        }
        var script = node.GetScript();
        if (script.VariantType != Variant.Type.Nil)
        {
            var scriptRes = script.As<Script>();
            if (scriptRes != null) dict["script"] = scriptRes.ResourcePath;
        }
        return dict;
    }

    private Dictionary PlayScene(Dictionary parms)
    {
        var scenePath = GetOr(parms,"scene_path", "").AsString();
        if (string.IsNullOrEmpty(scenePath))
            EditorInterface.Singleton.PlayMainScene();
        else
            EditorInterface.Singleton.PlayCustomScene(scenePath);
        return Success(new Dictionary { { "playing", true } });
    }

    private Dictionary StopScene()
    {
        if (!EditorInterface.Singleton.IsPlayingScene()) return Error("No scene is currently playing");
        EditorInterface.Singleton.StopPlayingScene();
        return Success(new Dictionary { { "stopped", true } });
    }
}
#endif
