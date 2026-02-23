import { z } from "zod";
import { registerGodotTool } from "../server.js";

registerGodotTool("runtime_get_scene_tree", "Get the live scene tree of the running game.", "runtime", "get_scene_tree", {
  depth: z.number().default(10).describe("Maximum tree depth to return"),
});
registerGodotTool("runtime_get_node_properties", "Get properties of a node in the running game.", "runtime", "get_node_properties", {
  node_path: z.string().describe("Node path in the running scene tree"),
});
registerGodotTool("runtime_capture_frame", "Capture a frame of performance data: FPS, frame time, object count, draw calls.", "runtime", "capture_frame");
registerGodotTool("runtime_monitor_property", "Monitor a property value over time.", "runtime", "monitor_property", {
  node_path: z.string().describe("Node path in running game"),
  property: z.string().describe("Property name to monitor"),
  duration: z.number().default(1000).describe("Monitoring duration in ms"),
});
