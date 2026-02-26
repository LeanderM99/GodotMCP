#if TOOLS
using Godot;
using Godot.Collections;
using System.Runtime.InteropServices;

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

        var projectName = ProjectSettings.GetSetting("application/config/name").AsString();
        if (string.IsNullOrEmpty(projectName)) projectName = "Godot";
        var editorPid = (uint)OS.GetProcessId();

        var gameHwnd = Win32Helper.FindGameWindow(projectName, editorPid);
        if (gameHwnd == System.IntPtr.Zero)
            return Error($"Could not find game window (looking for '{projectName}'). Make sure the game is running and visible.");

        // Capture the game window using PrintWindow
        Win32Helper.GetClientRect(gameHwnd, out var rect);
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
            return Error("Game window has zero size");

        var hdcWindow = Win32Helper.GetDC(gameHwnd);
        var hdcMem = Win32Helper.CreateCompatibleDC(hdcWindow);
        var hBitmap = Win32Helper.CreateCompatibleBitmap(hdcWindow, width, height);
        var hOld = Win32Helper.SelectObject(hdcMem, hBitmap);

        Win32Helper.PrintWindow(gameHwnd, hdcMem, Win32Helper.PW_RENDERFULLCONTENT);

        // Read pixel data from the bitmap
        var bmi = new Win32Helper.BITMAPINFO();
        bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<Win32Helper.BITMAPINFOHEADER>();
        bmi.bmiHeader.biWidth = width;
        bmi.bmiHeader.biHeight = -height; // top-down
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;
        bmi.bmiHeader.biCompression = 0;

        var pixelData = new byte[width * height * 4];
        Win32Helper.GetDIBits(hdcMem, hBitmap, 0, (uint)height, pixelData, ref bmi, 0);

        // Cleanup GDI objects
        Win32Helper.SelectObject(hdcMem, hOld);
        Win32Helper.DeleteObject(hBitmap);
        Win32Helper.DeleteDC(hdcMem);
        Win32Helper.ReleaseDC(gameHwnd, hdcWindow);

        // Convert BGRA → RGBA for Godot
        for (int i = 0; i < pixelData.Length; i += 4)
            (pixelData[i], pixelData[i + 2]) = (pixelData[i + 2], pixelData[i]);

        var image = Image.CreateFromData(width, height, false, Image.Format.Rgba8, pixelData);
        if (image == null || image.IsEmpty())
            return Error("Failed to create image from captured window data");

        if (image.GetWidth() > MaxScreenshotWidth)
        {
            var scale = (float)MaxScreenshotWidth / image.GetWidth();
            image.Resize((int)(image.GetWidth() * scale), (int)(image.GetHeight() * scale));
        }

        var savePath = ProjectSettings.GlobalizePath($"user://mcp_game_screenshot_{Time.GetTicksMsec()}.jpg");
        var err = image.SaveJpg(savePath, 0.85f);
        if (err != Godot.Error.Ok) return Error($"Failed to save game screenshot: {err}");
        return Success(new Dictionary { { "path", savePath }, { "format", "jpeg" }, { "width", width }, { "height", height } });
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
