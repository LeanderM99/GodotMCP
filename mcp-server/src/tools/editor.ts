import { z } from "zod";
import { registerGodotTool } from "../server.js";

registerGodotTool("editor_screenshot", "Take a screenshot of the editor viewport. Returns a base64 PNG image.", "editor", "screenshot",
  { viewport: z.enum(["2d", "3d", "full"]).default("full").describe("Which viewport to capture") },
);
registerGodotTool("editor_game_screenshot", "Take a screenshot of the running game viewport. Returns base64 PNG.", "editor", "game_screenshot",
);
registerGodotTool("editor_get_errors", "Get recent errors and warnings from the Godot output log.", "editor", "get_errors", {
  count: z.number().default(50).describe("Max number of log entries to return"),
});
registerGodotTool("editor_execute_gdscript", "Execute a GDScript expression in the editor context.", "editor", "execute_gdscript", {
  code: z.string().describe("GDScript code to execute"),
});
registerGodotTool("editor_execute_csharp", "Execute a C# expression in the editor context.", "editor", "execute_csharp", {
  code: z.string().describe("C# code/expression to execute"),
});
registerGodotTool("editor_reload_project", "Reload the current project in the editor.", "editor", "reload_project");
registerGodotTool("editor_get_open_files", "List all currently open files in the script editor.", "editor", "get_open_files");
registerGodotTool("editor_open_file", "Open a file in the script editor.", "editor", "open_file", {
  path: z.string().describe("Path to file to open"),
  line: z.number().optional().describe("Line number to jump to"),
});
