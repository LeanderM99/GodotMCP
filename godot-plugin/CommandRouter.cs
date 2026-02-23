#if TOOLS
using Godot;
using Godot.Collections;
using GodotMCP.Handlers;

namespace GodotMCP;

public class CommandRouter
{
    private readonly System.Collections.Generic.Dictionary<string, BaseHandler> _handlers = new();

    public void RegisterHandler(string category, BaseHandler handler)
    {
        _handlers[category] = handler;
    }

    public string Route(string rawMessage)
    {
        var json = Json.ParseString(rawMessage);
        if (json.VariantType == Variant.Type.Nil)
            return MakeError("invalid_json", "Failed to parse JSON");

        var msg = json.AsGodotDictionary();
        var id = msg.GetValueOrDefault("id", "unknown").AsString();
        var category = msg.GetValueOrDefault("category", "").AsString();
        var command = msg.GetValueOrDefault("command", "").AsString();
        var parms = msg.ContainsKey("params")
            ? msg["params"].AsGodotDictionary()
            : new Dictionary();

        if (!_handlers.TryGetValue(category, out var handler))
            return MakeResponse(id, false, null, $"Unknown category: {category}");

        try
        {
            var result = handler.Handle(command, parms);
            var success = result.ContainsKey("success") && result["success"].AsBool();
            var data = result.ContainsKey("data") ? result["data"] : new Dictionary();
            var error = result.ContainsKey("error") ? result["error"].AsString() : null;
            return MakeResponse(id, success, data, error);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[GodotMCP] Error handling {category}.{command}: {ex.Message}");
            return MakeResponse(id, false, null, ex.Message);
        }
    }

    private static string MakeResponse(string id, bool success, Variant? data, string error)
    {
        var dict = new Dictionary
        {
            { "id", id },
            { "success", success }
        };
        if (success && data.HasValue)
            dict["data"] = data.Value;
        if (!success && error != null)
            dict["error"] = error;
        return Json.Stringify(dict);
    }

    private static string MakeError(string id, string error) =>
        MakeResponse(id, false, null, error);
}
#endif
