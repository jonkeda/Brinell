# A stand-in for an IDE, to try the Scraper view in a browser: serves the view's page with a fake
# acquireVsCodeApi that relays the page's calls to the real language server over stdio.
#   python bridge.py <solution> <port>
import json, subprocess, sys, threading, queue, http.server

solution, port = sys.argv[1], int(sys.argv[2])
dll = r"E:\repos\MainConstruction2\construction\Bruinet\tools\Bruinet.Bdd.LanguageServer\bin\Debug\net10.0\Bruinet.Bdd.LanguageServer.dll"
server = subprocess.Popen(["dotnet", dll], stdin=subprocess.PIPE, stdout=subprocess.PIPE)
pending, events, lock, next_id = {}, [], threading.Lock(), [100]

def send(message):
    body = json.dumps(message).encode()
    server.stdin.write(b"Content-Length: %d\r\n\r\n" % len(body) + body)
    server.stdin.flush()

def reader():
    while True:
        length = None
        while True:
            line = server.stdout.readline()
            if not line: return
            line = line.strip()
            if not line: break
            if line.lower().startswith(b"content-length:"): length = int(line.split(b":")[1])
        message = json.loads(server.stdout.read(length))
        if "id" in message and ("result" in message or "error" in message):
            q = pending.pop(message["id"], None)
            if q: q.put(message)
        elif message.get("method") == "bruinet/scraper/changed":
            with lock: events.append(message["params"])
        elif "id" in message:  # a request from the server (workspace/configuration etc.)
            send({"jsonrpc": "2.0", "id": message["id"], "result": None})

threading.Thread(target=reader, daemon=True).start()

def request(method, params):
    with lock:
        next_id[0] += 1
        i = next_id[0]
    q = queue.Queue()
    pending[i] = q
    send({"jsonrpc": "2.0", "id": i, "method": method, "params": params})
    reply = q.get(timeout=600)
    return reply.get("result", {"problem": json.dumps(reply.get("error"))})

request("initialize", {"processId": None, "rootUri": None, "capabilities": {}, "initializationOptions": {"solutionDir": solution}})
send({"jsonrpc": "2.0", "method": "initialized", "params": {}})
page = request("bruinet/scraper/host", {})["html"]
shim = """<script>
window.acquireVsCodeApi = () => ({ postMessage: async (m) => {
  if (m.type === "call") {
    const r = await fetch("/call", { method: "POST", body: JSON.stringify(m) });
    window.postMessage({ type: "result", id: m.id, value: await r.json() }, "*");
  } else if (m.type === "ready") {
    setInterval(async () => { for (const e of await (await fetch("/events")).json()) window.postMessage({ ...e, type: "changed" }, "*"); }, 500);
  } else { console.log("to the IDE:", JSON.stringify(m)); window.__toIde = (window.__toIde || []).concat([m]); }
}});
</script>"""
page = page.replace("<head>", "<head>" + shim, 1).replace("script-src 'unsafe-inline'", "script-src 'unsafe-inline'; connect-src 'self'")

class Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, *a): pass
    def reply(self, body, kind="application/json"):
        data = body.encode()
        self.send_response(200); self.send_header("Content-Type", kind); self.send_header("Content-Length", str(len(data))); self.end_headers(); self.wfile.write(data)
    def do_GET(self):
        if self.path == "/events":
            with lock:
                out = list(events); events.clear()
            self.reply(json.dumps(out))
        else:
            self.reply(page, "text/html; charset=utf-8")
    def do_POST(self):
        m = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        self.reply(json.dumps(request(m["method"], m.get("params") or {})))

http.server.ThreadingHTTPServer(("127.0.0.1", port), Handler).serve_forever()
