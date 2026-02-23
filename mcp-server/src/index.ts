#!/usr/bin/env node

import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { server } from "./server.js";

// Import tool registrations (each file registers tools as a side effect)
import "./tools/project.js";
import "./tools/scene.js";
import "./tools/node.js";
import "./tools/script.js";
import "./tools/editor.js";
import "./tools/input.js";
import "./tools/runtime.js";

async function main() {
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error("Godot MCP server running on stdio");
}

main().catch((error) => {
  console.error("Fatal error:", error);
  process.exit(1);
});
