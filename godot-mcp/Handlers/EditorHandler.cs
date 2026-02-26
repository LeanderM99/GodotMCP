#if TOOLS
using Godot;
using Godot.Collections;

namespace GodotMCP.Handlers;

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

    private const int MaxScreenshotWidth = 1920;

    private Dictionary TakeScreenshot(Dictionary parms)
    {
        var viewport = GetOr(parms, "viewport", "full").AsString();
        switch (viewport)
        {
            case "2d": EditorInterface.Singleton.SetMainScreenEditor("2D"); break;
            case "3d": EditorInterface.Singleton.SetMainScreenEditor("3D"); break;
        }
        var editorViewport = EditorInterface.Singleton.GetBaseControl().GetViewport();
        var texRid = editorViewport.GetTexture().GetRid();
        var image = RenderingServer.Texture2DGet(texRid);
        if (image == null || image.IsEmpty()) return Error("Failed to capture viewport");

        // Resize for performance — keeps JPEG small and fast
        if (image.GetWidth() > MaxScreenshotWidth)
        {
            var scale = (float)MaxScreenshotWidth / image.GetWidth();
            image.Resize((int)(image.GetWidth() * scale), (int)(image.GetHeight() * scale));
        }

        var savePath = ProjectSettings.GlobalizePath($"user://mcp_screenshot_{Time.GetTicksMsec()}.jpg");
        var err = image.SaveJpg(savePath, 0.85f);
        if (err != Godot.Error.Ok) return Error($"Failed to save screenshot: {err}");
        return Success(new Dictionary { { "path", savePath }, { "format", "jpeg" } });
    }

    private Dictionary TakeGameScreenshot()
    {
        if (!EditorInterface.Singleton.IsPlayingScene())
            return Error("No game is currently running");

        // The game runs as a separate child process — the editor cannot directly
        // access its viewport.  We capture the editor viewport which shows the
        // embedded game view when "run in editor" is enabled.
        var editorViewport = EditorInterface.Singleton.GetBaseControl().GetViewport();
        var texRid = editorViewport.GetTexture().GetRid();
        var image = RenderingServer.Texture2DGet(texRid);
        if (image == null || image.IsEmpty())
            return Error("Failed to capture viewport. Note: the game runs as a separate process; this tool captures the editor viewport.");

        if (image.GetWidth() > MaxScreenshotWidth)
        {
            var scale = (float)MaxScreenshotWidth / image.GetWidth();
            image.Resize((int)(image.GetWidth() * scale), (int)(image.GetHeight() * scale));
        }

        var savePath = ProjectSettings.GlobalizePath($"user://mcp_game_screenshot_{Time.GetTicksMsec()}.jpg");
        var err = image.SaveJpg(savePath, 0.85f);
        if (err != Godot.Error.Ok) return Error($"Failed to save screenshot: {err}");
        return Success(new Dictionary { { "path", savePath }, { "format", "jpeg" }, { "note", "Captures the editor viewport. The game runs as a separate process so its window cannot be directly captured." } });
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
