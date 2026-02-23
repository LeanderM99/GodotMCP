import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z, ZodRawShape } from "zod";
import { GodotConnection } from "./godot-connection.js";

export const godot = new GodotConnection(
  parseInt(process.env.GODOT_MCP_PORT || "6550", 10)
);

export const server = new McpServer({
  name: "godot-mcp-server",
  version: "0.1.0",
});

export function registerGodotTool(
  name: string,
  description: string,
  category: string,
  command: string,
  inputSchema?: ZodRawShape,
  options?: {
    isImage?: boolean;
    transform?: (data: unknown) => unknown;
  }
) {
  const handler = async (params: Record<string, unknown>) => {
    try {
      const data = await godot.send(category, command, params);
      const result = options?.transform ? options.transform(data) : data;

      if (options?.isImage && typeof result === "string") {
        return {
          content: [{ type: "image" as const, data: result, mimeType: "image/png" }],
        };
      }

      const text = typeof result === "string" ? result : JSON.stringify(result, null, 2);
      return { content: [{ type: "text" as const, text }] };
    } catch (err) {
      return {
        isError: true,
        content: [{ type: "text" as const, text: `Error: ${err instanceof Error ? err.message : String(err)}` }],
      };
    }
  };

  if (inputSchema) {
    server.registerTool(name, { description, inputSchema }, handler);
  } else {
    server.registerTool(name, { description }, handler as () => ReturnType<typeof handler>);
  }
}
