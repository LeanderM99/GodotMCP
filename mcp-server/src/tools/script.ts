import { z } from "zod";
import { registerGodotTool } from "../server.js";

registerGodotTool("script_list", "List all scripts in the project.", "script", "list", {
  path: z.string().default("res://").describe("Directory to search in"),
  language: z.enum(["cs", "gd", "all"]).default("all").describe("Filter by language"),
});
registerGodotTool("script_read", "Read a script file's source code.", "script", "read", {
  path: z.string().describe("Script path (e.g. 'res://scenes/Player.cs')"),
});
registerGodotTool("script_create", "Create a new script file.", "script", "create", {
  path: z.string().describe("Path for new script"),
  content: z.string().describe("Script source code"),
  language: z.enum(["cs", "gd"]).default("cs").describe("Script language"),
});
registerGodotTool("script_edit", "Replace the contents of an existing script file.", "script", "edit", {
  path: z.string().describe("Script path to edit"),
  content: z.string().describe("New source code"),
});
registerGodotTool("script_attach", "Attach a script to a node in the current scene.", "script", "attach", {
  node_path: z.string().describe("Path to the node"),
  script_path: z.string().describe("Path to the script file"),
});
registerGodotTool("script_detach", "Detach the script from a node.", "script", "detach", {
  node_path: z.string().describe("Path to the node"),
});
