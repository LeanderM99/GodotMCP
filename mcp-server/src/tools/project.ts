import { z } from "zod";
import { registerGodotTool } from "../server.js";

registerGodotTool(
  "project_get_settings",
  "Read project.godot settings. Returns all settings if no section/key specified.",
  "project", "get_settings",
  {
    section: z.string().optional().describe("Settings section (e.g. 'application')"),
    key: z.string().optional().describe("Specific key within section"),
  }
);

registerGodotTool(
  "project_list_files",
  "List files in the project directory. Supports glob-like filtering.",
  "project", "list_files",
  {
    path: z.string().default("res://").describe("Directory path to list (e.g. 'res://scenes/')"),
    filter: z.string().optional().describe("File extension filter (e.g. '*.tscn', '*.cs')"),
    recursive: z.boolean().default(false).describe("List files recursively"),
  }
);

registerGodotTool("project_read_file", "Read the contents of a project file.", "project", "read_file", {
  path: z.string().describe("Resource path (e.g. 'res://scenes/Player.tscn')"),
});

registerGodotTool("project_write_file", "Write or create a file in the project.", "project", "write_file", {
  path: z.string().describe("Resource path to write to"),
  content: z.string().describe("File content"),
});

registerGodotTool("project_get_uid", "Convert a resource path to its UID.", "project", "get_uid", {
  path: z.string().describe("Resource path (e.g. 'res://scenes/Player.tscn')"),
});

registerGodotTool("project_get_path_from_uid", "Convert a UID back to a resource path.", "project", "get_path_from_uid", {
  uid: z.string().describe("Resource UID string"),
});
