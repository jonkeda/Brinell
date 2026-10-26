import json, subprocess, sys
# Runs MCP tool calls against the Bruinet MCP server, one process per run: python mcp.py <solution> <calls.json>
solution, calls = sys.argv[1], json.load(open(sys.argv[2], encoding='utf-8'))
lines = [json.dumps({"jsonrpc":"2.0","id":0,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"acc","version":"1"}}})]
for i, (name, args) in enumerate(calls, 1):
    lines.append(json.dumps({"jsonrpc":"2.0","id":i,"method":"tools/call","params":{"name":name,"arguments":args}}))
dll = r"E:\repos\MainConstruction2\construction\Bruinet\tools\Bruinet.Bdd.LanguageServer\bin\Debug\net10.0\Bruinet.Bdd.LanguageServer.dll"
out = subprocess.run(["dotnet", dll, "--mcp", "--solution", solution], input="\n".join(lines)+"\n", capture_output=True, text=True, encoding='utf-8').stdout
for line in out.splitlines():
    msg = json.loads(line)
    if msg["id"] == 0: continue
    r = msg.get("result", msg.get("error"))
    name = calls[msg["id"]-1][0]
    print(f"=== {name} {'ERROR' if r.get('isError') else ''}")
    print(r["content"][0]["text"] if "content" in r else r)
