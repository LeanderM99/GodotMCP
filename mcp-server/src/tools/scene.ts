import { z } from "zod";
import { registerGodotTool } from "../server.js";

registerGodotTool("scene_create", "Create a new scene file with a root node of the specified type.", "scene", "create", {
  path: z.string().describe("Scene file path (e.g. 'res://scenes/NewScene.tscn')"),
  root_type: z.string().describe("Root node type (e.g. 'Node2D', 'CharacterBody2D')"),
  root_name: z.string().optional().describe("Root node name (defaults to filename)"),
});
registerGodotTool("scene_open", "Open a scene in the editor.", "scene", "open", {
  path: z.string().describe("Scene path to open"),
});
registerGodotTool("scene_get_current", "Get info about the currently open scene.", "scene", "get_current");
registerGodotTool("scene_save", "Save the current or specified scene.", "scene", "save", {
  path: z.string().optional().describe("Scene path (saves current scene if omitted)"),
});
registerGodotTool("scene_delete", "Delete a scene file from the project.", "scene", "delete", {
  path: z.string().describe("Scene path to delete"),
});
registerGodotTool("scene_instance", "Instance a PackedScene as a child of a node in the current scene.", "scene", "instance", {
  scene_path: z.string().describe("Path to the scene to instance"),
  parent_path: z.string().describe("Node path of the parent"),
  name: z.string().optional().describe("Name for the instanced node"),
});
registerGodotTool("scene_get_tree", "Get the full scene tree structure as JSON.", "scene", "get_tree", {
  path: z.string().optional().describe("Scene path (uses current scene if omitted)"),
});
registerGodotTool("scene_play", "Run the project or a specific scene.", "scene", "play", {
  scene_path: z.string().optional().describe("Scene to play (plays main scene if omitted)"),
});
registerGodotTool("scene_stop", "Stop the currently running game.", "scene", "stop");
