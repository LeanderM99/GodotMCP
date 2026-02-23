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

    private Dictionary GetSettings(Dictionary parms)
    {
        var section = parms.GetValueOrDefault("section", "").AsString();
        var key = parms.GetValueOrDefault("key", "").AsString();

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
        foreach (var prop in ProjectSettings.GetPropertyList())
        {
            var propDict = prop.AsGodotDictionary();
            var name = propDict["name"].AsString();
            if (string.IsNullOrEmpty(section) || name.StartsWith(section + "/"))
                settings[name] = ProjectSettings.GetSetting(name);
        }
        return Success(new Dictionary { { "settings", settings } });
    }

    private Dictionary ListFiles(Dictionary parms)
    {
        var path = parms.GetValueOrDefault("path", "res://").AsString();
        var filter = parms.GetValueOrDefault("filter", "").AsString();
        var recursive = parms.GetValueOrDefault("recursive", false).AsBool();

        var files = new Array();
        ListFilesRecursive(path, filter, recursive, files);
        return Success(new Dictionary { { "files", files } });
    }

    private void ListFilesRecursive(string path, string filter, bool recursive, Array files)
    {
        var dir = DirAccess.Open(path);
        if (dir == null) return;

        dir.ListDirBegin();
        var fileName = dir.GetNext();
        while (!string.IsNullOrEmpty(fileName))
        {
            if (dir.CurrentIsDir())
            {
                if (recursive && !fileName.StartsWith("."))
                    ListFilesRecursive(path.TrimEnd('/') + "/" + fileName, filter, true, files);
            }
            else
            {
                if (string.IsNullOrEmpty(filter) || MatchesFilter(fileName, filter))
                    files.Add(path.TrimEnd('/') + "/" + fileName);
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
