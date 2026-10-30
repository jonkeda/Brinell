# Claude's part of the manual test plan, scripted

From the Bruinet root, with `tools\Bruinet.Bdd.LanguageServer` built:

1. Start the Blazor sample on http://localhost:5180 (`dotnet run --project samples\Bruinet.Samples.Blazor.App --urls http://localhost:5180`).
2. Make a scratch folder `<scratch>` with `Shop.UITests\` (a `.csproj` referencing `src\Bruinet.Html`,
   and a `scraper.config.md`: `StartUrl` `http://localhost:8766/index.html`, a mask `P-100` to
   `SKU-MASKED`), a copy of `tests\Bruinet.Scraper.Tests\TestSite` as `<scratch>\site` served on
   port 8766 (`python -m http.server 8766`), and an empty folder.
3. Start three stand-in hosts, each serving the view and relaying to its own language server:
   - `BRUINET_SCRAPER_DEBUG_PORT=9333 python bridge.py <Bruinet root> 8791`
   - `BRUINET_SCRAPER_DEBUG_PORT=9334 python bridge.py <scratch> 8792`
   - `python bridge.py <empty folder> 8793`
4. `dotnet run -- <Bruinet root> <scratch> all`; then write a page object for `/pages/catalog.html`
   over MCP (`mcp.py`, A-3), and `dotnet run -- <Bruinet root> <scratch> refresh` (F-1).
5. Afterwards `git clean -fd tests\Bruinet.Blazor.UITests\Recordings`.

Nothing else may open the stand-in hosts' pages meanwhile: each page takes the server's notices.
`bridge.py` and `mcp.py` have the server's path in them (`tools\...\bin\Debug\net10.0`).
