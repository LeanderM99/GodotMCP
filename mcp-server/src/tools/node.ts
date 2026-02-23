import { z } from "zod";
import { registerGodotTool } from "../server.js";

registerGodotTool("node_add", "Add a new node to the scene tree.", "node", "add", {
  parent_path: z.string().describe("Path to parent node"),
  type: z.string().describe("Node type (e.g. 'CharacterBody2D', 'Sprite2D')"),
  name: z.string().describe("Name for the new node"),
});
registerGodotTool("node_delete", "Remove a node from the scene tree.", "node", "delete", {
  node_path: z.string().describe("Path to node to delete"),
});
registerGodotTool("node_rename", "Rename a node.", "node", "rename", {
  node_path: z.string().describe("Path to node to rename"),
  new_name: z.string().describe("New name for the node"),
});
registerGodotTool("node_duplicate", "Duplicate a node and all its children.", "node", "duplicate", {
  node_path: z.string().describe("Path to node to duplicate"),
  new_name: z.string().optional().describe("Name for the duplicate"),
});
registerGodotTool("node_move", "Reparent a node to a new parent.", "node", "move", {
  node_path: z.string().describe("Path to node to move"),
  new_parent_path: z.string().describe("Path to new parent node"),
});
registerGodotTool("node_get_properties", "Get all properties of a node.", "node", "get_properties", {
  node_path: z.string().describe("Path to node"),
});
registerGodotTool("node_set_property", "Set a property on a node.", "node", "set_property", {
  node_path: z.string().describe("Path to node"),
  property: z.string().describe("Property name (e.g. 'position', 'visible')"),
  value: z.unknown().describe("Value to set (type depends on property)"),
});
registerGodotTool("node_get_signals", "List all signals defined on a node.", "node", "get_signals", {
  node_path: z.string().describe("Path to node"),
});
registerGodotTool("node_connect_signal", "Connect a signal to a method on another node.", "node", "connect_signal", {
  node_path: z.string().describe("Path to node emitting the signal"),
  signal: z.string().describe("Signal name"),
  target_path: z.string().describe("Path to target node"),
  method: z.string().describe("Method name on target"),
});
registerGodotTool("node_disconnect_signal", "Disconnect a signal connection.", "node", "disconnect_signal", {
  node_path: z.string().describe("Path to node emitting the signal"),
  signal: z.string().describe("Signal name"),
  target_path: z.string().describe("Path to target node"),
  method: z.string().describe("Method name on target"),
});
registerGodotTool("node_get_children", "List immediate children of a node.", "node", "get_children", {
  node_path: z.string().describe("Path to parent node"),
});
