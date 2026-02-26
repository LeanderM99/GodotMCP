#if TOOLS
using Godot;
using Godot.Collections;

namespace GodotMCP.Handlers;

public class ProjectHandler : BaseHandler
{
    public ProjectHandler(EditorPlugin plugin) : base(plugin) { }

    public override Dictionary Handle(string command, Dictionary parms)
    {
        return command switch
        {
            "get_settings" => GetSettings(parms),
            "list_files" => ListFiles(parms),
            "read_file" => ReadFile(parms),
            "write_file" => WriteFile(parms),
            "get_uid" => GetUid(parms),
            "get_path_from_uid" => GetPathFromUid(parms),
            _ => Error($"Unknown project command: {command}")
        };
    }

    private const int MaxSettingsResults = 500;

    private Dictionary GetSettings(Dictionary parms)
    {
        var section = GetOr(parms,"section", "").AsString();
        var key = GetOr(parms,"key", "").AsString();

        if (!string.IsNullOrEmpty(section) && !string.IsNullOrEmpty(key))
        {
            var settingPath = $"{section}/{key}";
            if (ProjectSettings.HasSetting(settingPath))
            {
                var value = ProjectSettings.GetSetting(settingPath);
                return Success(new Dictionary { { "value", value } });
            }
            return Error($"Setting not found: {settingPath}");
        }

        var settings = new Dictionary();
        var ps = (GodotObject)Engine.GetSingleton("ProjectSettings");
        var prefix = string.IsNullOrEmpty(section) ? "" : section + "/";
        int count = 0;
        bool truncated = false;
        foreach (var propDict in ps.GetPropertyList())
        {
            var name = propDict["name"].AsString();
            // Skip internal/metadata properties that bloat results
            if (name.StartsWith("_") || name.StartsWith("editor_plugins/"))
                continue;
            if (string.IsNullOrEmpty(prefix) || name.StartsWith(prefix))
            {
                settings[name] = ProjectSettings.GetSetting(name);
                if (++count >= MaxSettingsResults)
                {
                    truncated = true;
                    break;
                }
            }
        }
        var result = new Dictionary { { "settings", settings } };
        if (truncated)
            result["truncated"] = true;
        return Success(result);
    }

    private const int MaxFileResults = 5000;

    private static readonly System.Collections.Generic.HashSet<string> SkipDirs = new(System.StringComparer.OrdinalIgnoreCase)
    {
        ".godot", ".import", ".mono", ".vs", "bin", "obj"
    };

    private Dictionary ListFiles(Dictionary parms)
    {
        var path = GetOr(parms,"path", "res://").AsString();
        var filter = GetOr(parms,"filter", "").AsString();
        var recursive = GetOr(parms,"recursive", false).AsBool();

        var files = new Array();
        bool truncated = false;
        ListFilesRecursive(path, filter, recursive, files, ref truncated);
        var result = new Dictionary { { "files", files } };
        if (truncated)
            result["truncated"] = true;
        return Success(result);
    }

    private void ListFilesRecursive(string path, string filter, bool recursive, Array files, ref bool truncated)
    {
        if (files.Count >= MaxFileResults)
        {
            truncated = true;
            return;
        }

        var dir = DirAccess.Open(path);
        if (dir == null) return;

        dir.ListDirBegin();
        var fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            if (dir.CurrentIsDir())
            {
                if (recursive && !fileName.StartsWith(".") && !SkipDirs.Contains(fileName))
                    ListFilesRecursive(path.TrimEnd('/') + "/" + fileName, filter, true, files, ref truncated);
            }
            else
            {
                if (string.IsNullOrEmpty(filter) || MatchesFilter(fileName, filter))
                    files.Add(path.TrimEnd('/') + "/" + fileName);
            }
            if (files.Count >= MaxFileResults)
            {
                truncated = true;
                break;
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();
    }

    private static bool MatchesFilter(string fileName, string filter)
    {
        if (filter.StartsWith("*."))
            return fileName.EndsWith(filter[1..], System.StringComparison.OrdinalIgnoreCase);
        return fileName.Contains(filter, System.StringComparison.OrdinalIgnoreCase);
    }

    private Dictionary ReadFile(Dictionary parms)
    {
        var path = parms["path"].AsString();
        if (!FileAccess.FileExists(path))
            return Error($"File not found: {path}");
        var content = FileAccess.GetFileAsString(path);
        return Success(new Dictionary { { "content", content }, { "path", path } });
    }

    private Dictionary WriteFile(Dictionary parms)
    {
        var path = parms["path"].AsString();
        var content = parms["content"].AsString();

        var dir = path[..path.LastIndexOf('/')];
        DirAccess.MakeDirRecursiveAbsolute(dir);

        var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
            return Error($"Cannot write to: {path} ({FileAccess.GetOpenError()})");
        file.StoreString(content);
        file.Close();

        EditorInterface.Singleton.GetResourceFilesystem().Scan();
        return Success(new Dictionary { { "path", path } });
    }

    private Dictionary GetUid(Dictionary parms)
    {
        var path = parms["path"].AsString();
        var uid = ResourceLoader.GetResourceUid(path);
        if (uid == -1)
            return Error($"No UID found for: {path}");
        return Success(new Dictionary { { "uid", ResourceUid.IdToText(uid) }, { "path", path } });
    }

    private Dictionary GetPathFromUid(Dictionary parms)
    {
        var uidStr = parms["uid"].AsString();
        var uid = ResourceUid.TextToId(uidStr);
        if (uid == -1)
            return Error($"Invalid UID: {uidStr}");
        if (!ResourceUid.HasId(uid))
            return Error($"UID not found: {uidStr}");
        var path = ResourceUid.GetIdPath(uid);
        return Success(new Dictionary { { "path", path }, { "uid", uidStr } });
    }
}
#endif
