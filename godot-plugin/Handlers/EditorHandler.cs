#if TOOLS
using Godot;
using Godot.Collections;

namespace GodotMCP.Handlers;

using Convert = System.Convert;
using Math = System.Math;

public class EditorHandler : BaseHandler
{
    private readonly Array _errorLog = new();

    public EditorHandler(EditorPlugin plugin) : base(plugin) { }

    public override Dictionary Handle(string command, Dictionary parms)
    {
        return command switch
        {
            "screenshot" => TakeScreenshot(parms),
            "game_screenshot" => TakeGameScreenshot(),
            "get_errors" => GetErrors(parms),
            "execute_gdscript" => ExecuteGdScript(parms),
            "execute_csharp" => ExecuteCSharp(parms),
            "reload_project" => ReloadProject(),
            "get_open_files" => GetOpenFiles(),
            "open_file" => OpenFile(parms),
            _ => Error($"Unknown editor command: {command}")
        };
    }

    private Dictionary TakeScreenshot(Dictionary parms)
    {
        var viewport = GetOr(parms, "viewport", "full").AsString();
        switch (viewport)
        {
            case "2d": EditorInterface.Singleton.SetMainScreenEditor("2d"); break;
            case "3d": EditorInterface.Singleton.SetMainScreenEditor("3d"); break;
        }
        var editorViewport = EditorInterface.Singleton.GetBaseControl().GetViewport();
        var image = editorViewport.GetTexture().GetImage();
        if (image == null) return Error("Failed to capture viewport");
        var pngData = image.SavePngToBuffer();
        var base64 = Convert.ToBase64String(pngData);
        return Success(new Dictionary { { "image", base64 }, { "format", "png" } });
    }

    private Dictionary TakeGameScreenshot()
    {
        if (!EditorInterface.Singleton.IsPlayingScene())
            return Error("No game is currently running");
        var editorViewport = EditorInterface.Singleton.GetBaseControl().GetViewport();
        var image = editorViewport.GetTexture().GetImage();
        if (image == null) return Error("Failed to capture game viewport");
        var pngData = image.SavePngToBuffer();
        var base64 = Convert.ToBase64String(pngData);
        return Success(new Dictionary { { "image", base64 }, { "format", "png" } });
    }

    private Dictionary GetErrors(Dictionary parms)
    {
        var count = GetOr(parms, "count", 50).AsInt32();
        var errors = new Array();
        var startIdx = Math.Max(0, _errorLog.Count - count);
        for (int i = startIdx; i < _errorLog.Count; i++)
            errors.Add(_errorLog[i]);
        return Success(new Dictionary { { "errors", errors }, { "count", errors.Count } });
    }

    public void LogError(string message, string type = "error")
    {
        _errorLog.Add(new Dictionary { { "message", message }, { "type", type }, { "timestamp", Time.GetTicksMsec() } });
        while (_errorLog.Count > 500) _errorLog.RemoveAt(0);
    }

    private Dictionary ExecuteGdScript(Dictionary parms)
    {
        var code = parms["code"].AsString();
        var expression = new Expression();
        var err = expression.Parse(code);
        if (err != Godot.Error.Ok) return Error($"Parse error: {expression.GetErrorText()}");
        var result = expression.Execute();
        if (expression.HasExecuteFailed())
            return Error($"Execution error: {expression.GetErrorText()}");
        return Success(new Dictionary { { "result", result.ToString() } });
    }

    private Dictionary ExecuteCSharp(Dictionary parms)
    {
        return Error("C# expression execution requires Roslyn scripting. Use editor_execute_gdscript for dynamic code or script_edit for modifying .cs files.");
    }

    private Dictionary ReloadProject()
    {
        EditorInterface.Singleton.RestartEditor(true);
        return Success(new Dictionary { { "reloading", true } });
    }

    private Dictionary GetOpenFiles()
    {
        var scriptEditor = EditorInterface.Singleton.GetScriptEditor();
        var openScripts = scriptEditor.GetOpenScripts();
        var files = new Array();
        foreach (var script in openScripts)
            files.Add(new Dictionary { { "path", script.ResourcePath }, { "type", script.GetClass() } });
        return Success(new Dictionary { { "files", files }, { "count", files.Count } });
    }

    private Dictionary OpenFile(Dictionary parms)
    {
        var path = parms["path"].AsString();
        var line = GetOr(parms, "line", 0).AsInt32();
        var script = ResourceLoader.Load<Script>(path);
        if (script != null)
        {
            EditorInterface.Singleton.EditScript(script, line);
            return Success(new Dictionary { { "path", path }, { "line", line } });
        }
        EditorInterface.Singleton.OpenSceneFromPath(path);
        return Success(new Dictionary { { "path", path } });
    }
}
#endif
