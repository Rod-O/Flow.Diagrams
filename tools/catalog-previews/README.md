# Catalog previews

Smoke-checks every example of `samples/Catalog` in a headless browser and writes the gallery's preview images.

```
dotnet publish samples/Catalog -c Release -o <out>
# serve <out>/wwwroot as a single-page app (unknown paths fall back to index.html), then:
npm install && npx playwright install chromium
node previews.cjs http://localhost:<port>/ ../../samples/Catalog/wwwroot/previews
```

Without the second argument it only checks. It exits non-zero when an example logs an error, fails a request, shows
Blazor's error bar, draws nothing, or draws a link with NaN coordinates.
