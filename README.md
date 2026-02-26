# Godot MCP

An [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) server that connects AI assistants like Claude to the Godot 4.6+ editor. Control scenes, nodes, scripts, and even run your game — all from your AI coding tool.

Built for **Godot .NET (C#)**.

## Getting Started

### Prerequisites

- [Godot 4.6+](https://godotengine.org/) with .NET / C# support
- [Node.js](https://nodejs.org/) 18+

### 1. Install the Godot Plugin

Copy the `godot-mcp/` folder into your Godot project's `addons/` directory:

```
your-project/
  addons/
    godot-mcp/
      MCPPlugin.cs
      WebSocketServer.cs
      CommandRouter.cs
      Handlers/
      plugin.cfg
```

Then enable the plugin in **Project > Project Settings > Plugins** — check **Godot MCP**.

### 2. Build the MCP Server

```bash
cd mcp-server
npm install
npm run build
```

### 3. Configure Your AI Tool

Add the server to your MCP client config. For **Claude Code**, add to your `.claude/settings.json`:

```json
{
  "mcpServers": {
    "godot": {
      "command": "node",
      "args": ["<path-to>/mcp-server/build/index.js"]
    }
  }
}
```

### 4. Open Godot and Start Using

Open your Godot project in the editor. The plugin starts a WebSocket server on port `6550` automatically. The MCP server connects to it when your AI tool makes its first request.

### Environment Variables

| Variable | Default | Description |
|---|---|---|
| `GODOT_MCP_PORT` | `6550` | WebSocket port the MCP server connects to |
| `GODOT_MCP_TIMEOUT` | `15000` | Request timeout in milliseconds |

## Architecture

```
AI Tool (Claude Code, etc.)
  ↕ stdio (MCP protocol)
MCP Server (Node.js / TypeScript)
  ↕ WebSocket (localhost:6550)
Godot Editor Plugin (C# EditorPlugin)
  ↕ Godot API
Your Game Project
```

Two components:

- **`mcp-server/`** — TypeScript MCP server using stdio transport. Validates inputs with Zod, forwards commands over WebSocket.
- **`godot-mcp/`** — C# EditorPlugin that runs inside the Godot editor. Receives commands via WebSocket, executes them against the Godot API, returns results.

## Tools

45 tools across 7 categories.

### Project

| Tool | Description |
|---|---|
| `project_get_settings` | Read project.godot settings. Returns all settings if no section/key specified. |
| `project_list_files` | List files in a directory. Supports glob-like filtering and recursive scan. |
| `project_read_file` | Read the contents of a project file. |
| `project_write_file` | Write or create a file in the project. |
| `project_get_uid` | Convert a resource path to its UID. |
| `project_get_path_from_uid` | Convert a UID back to a resource path. |

### Scene

| Tool | Description |
|---|---|
| `scene_create` | Create a new scene file with a root node of the specified type. |
| `scene_open` | Open a scene in the editor. |
| `scene_get_current` | Get info about the currently open scene. |
| `scene_save` | Save the current or specified scene. |
| `scene_delete` | Delete a scene file from the project. |
| `scene_instance` | Instance a PackedScene as a child of a node in the current scene. |
| `scene_get_tree` | Get the full scene tree structure as JSON. |
| `scene_play` | Run the project or a specific scene. |
| `scene_stop` | Stop the currently running game. |

### Node

| Tool | Description |
|---|---|
| `node_add` | Add a new node to the scene tree. |
| `node_delete` | Remove a node from the scene tree. |
| `node_rename` | Rename a node. |
| `node_duplicate` | Duplicate a node and all its children. |
| `node_move` | Reparent a node to a new parent. |
| `node_get_properties` | Get all properties of a node. |
| `node_set_property` | Set a property on a node. |
| `node_get_signals` | List all signals defined on a node. |
| `node_connect_signal` | Connect a signal to a method on another node. |
| `node_disconnect_signal` | Disconnect a signal connection. |
| `node_get_children` | List immediate children of a node. |

### Script

| Tool | Description |
|---|---|
| `script_list` | List all scripts in the project. Filter by language (C#, GDScript, or both). |
| `script_read` | Read a script file's source code. |
| `script_create` | Create a new script file. |
| `script_edit` | Replace the contents of an existing script file. |
| `script_attach` | Attach a script to a node in the current scene. |
| `script_detach` | Detach the script from a node. |

### Editor

| Tool | Description |
|---|---|
| `editor_screenshot` | Take a screenshot of the editor viewport (2D, 3D, or full). |
| `editor_game_screenshot` | Take a screenshot of the running game viewport. |
| `editor_get_errors` | Get recent errors and warnings from the Godot output log. |
| `editor_execute_gdscript` | Execute a GDScript expression in the editor context. |
| `editor_execute_csharp` | Execute a C# expression in the editor context. |
| `editor_reload_project` | Reload the current project in the editor. |
| `editor_get_open_files` | List all currently open files in the script editor. |
| `editor_open_file` | Open a file in the script editor at an optional line number. |

### Input

| Tool | Description |
|---|---|
| `input_key` | Simulate a keyboard key press/release in the running game. |
| `input_mouse` | Simulate a mouse event (click, press, release, move) in the running game. |
| `input_action` | Trigger a Godot input action (e.g. `move_up`, `jump`). |
| `input_text` | Type a text string character by character. |
| `input_sequence` | Execute a timed sequence of input actions. |

### Runtime

| Tool | Description |
|---|---|
| `runtime_get_scene_tree` | Get the live scene tree of the running game. |
| `runtime_get_node_properties` | Get properties of a node in the running game. |
| `runtime_capture_frame` | Capture a frame of performance data: FPS, frame time, object count, draw calls. |
| `runtime_monitor_property` | Monitor a property value over time. |

## License

MIT
