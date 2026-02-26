import WebSocket from "ws";

interface GodotCommand {
  id: string;
  category: string;
  command: string;
  params: Record<string, unknown>;
}

interface GodotResponse {
  id: string;
  success: boolean;
  data?: unknown;
  error?: string;
}

export class GodotConnection {
  private ws: WebSocket | null = null;
  private pendingRequests = new Map<string, {
    resolve: (value: GodotResponse) => void;
    reject: (reason: Error) => void;
    timer: ReturnType<typeof setTimeout>;
  }>();
  private port: number;
  private requestTimeout: number;

  constructor(port = 6550, requestTimeout = 60000) {
    this.port = port;
    this.requestTimeout = requestTimeout;
  }

  async connect(): Promise<void> {
    if (this.ws?.readyState === WebSocket.OPEN) return;

    return new Promise((resolve, reject) => {
      this.ws = new WebSocket(`ws://127.0.0.1:${this.port}`);

      this.ws.on("open", () => {
        console.error(`[GodotMCP] Connected to Godot on port ${this.port}`);
        resolve();
      });

      this.ws.on("message", (data) => {
        const text = data.toString();
        try {
          const response: GodotResponse = JSON.parse(text);
          const pending = this.pendingRequests.get(response.id);
          if (pending) {
            clearTimeout(pending.timer);
            this.pendingRequests.delete(response.id);
            pending.resolve(response);
          }
        } catch (e) {
          console.error("[GodotMCP] Failed to parse response:", text);
        }
      });

      this.ws.on("close", () => {
        console.error("[GodotMCP] Disconnected from Godot");
        this.ws = null;
        for (const [id, pending] of this.pendingRequests) {
          clearTimeout(pending.timer);
          pending.reject(new Error("Connection closed"));
        }
        this.pendingRequests.clear();
      });

      this.ws.on("error", (err) => {
        reject(new Error(
          `Cannot connect to Godot on port ${this.port}. ` +
          `Make sure the Godot editor is running with the MCP plugin enabled. ` +
          `Error: ${err.message}`
        ));
      });
    });
  }

  async send(category: string, command: string, params: Record<string, unknown> = {}): Promise<unknown> {
    await this.connect();

    const id = crypto.randomUUID();
    const message: GodotCommand = { id, category, command, params };

    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        this.pendingRequests.delete(id);
        reject(new Error(`Command ${category}.${command} timed out after ${this.requestTimeout}ms`));
      }, this.requestTimeout);

      this.pendingRequests.set(id, {
        resolve: (response) => {
          if (response.success) {
            resolve(response.data);
          } else {
            reject(new Error(response.error || "Unknown error from Godot"));
          }
        },
        reject,
        timer,
      });

      this.ws!.send(JSON.stringify(message));
    });
  }

  disconnect(): void {
    this.ws?.close();
    this.ws = null;
  }
}
