#!/usr/bin/env python3
"""
Crea un sitio web standalone (un solo archivo HTML) del proyecto Aethon.
Estrategia:
1. Captura el HTML renderizado de localhost:3000 con agent-browser.
2. Embebe el CSS inline.
3. Convierte las imágenes de /cosmic/ a base64 data URIs.
4. Empaqueta todo en un único .html autocontenido.
"""
import base64
import re
import subprocess
import sys
from pathlib import Path

# Step 1: Fetch the rendered page via agent-browser (full HTML)
print("→ Fetching rendered page from localhost:3000...")
subprocess.run(
    ["agent-browser", "set", "viewport", "1440", "900"],
    check=False, capture_output=True
)
subprocess.run(
    ["agent-browser", "open", "http://localhost:3000"],
    check=False, capture_output=True
)
subprocess.run(["agent-browser", "wait", "2000"], check=False, capture_output=True)

# Get the full HTML
result = subprocess.run(
    ["agent-browser", "eval",
     "document.documentElement.outerHTML"],
    capture_output=True, text=True, check=False
)
html = result.stdout.strip().strip('"')
# Unescape JSON string escaping
html = html.replace("\\n", "\n").replace('\\"', '"').replace("\\/", "/").replace("\\\\", "\\")
print(f"  HTML length: {len(html)}")

# Step 2: Read the CSS from globals.css and inline it
css_path = "/home/z/my-project/src/app/globals.css"
with open(css_path) as f:
    css = f.read()
# Also need Tailwind's compiled CSS — fetch from the page's <style> tags + linked stylesheets
# The dev server injects styles via JS; for a standalone file we need the compiled CSS.
# Strategy: fetch all <style> tags content + fetch linked CSS files.

# Extract <style> contents from the HTML
style_blocks = re.findall(r"<style[^>]*>(.*?)</style>", html, re.DOTALL)
inline_css = "\n".join(style_blocks)
print(f"  Inline <style> blocks: {len(style_blocks)}, total {len(inline_css)} chars")

# Step 3: Embed images as base64
cosmic_dir = Path("/home/z/my-project/public/cosmic")
image_map = {}
for img in cosmic_dir.glob("*.png"):
    with open(img, "rb") as f:
        b64 = base64.b64encode(f.read()).decode()
    image_map[f"/cosmic/{img.name}"] = f"data:image/png;base64,{b64}"
    print(f"  Embedded: /cosmic/{img.name} ({img.stat().st_size // 1024} KB)")

# Also embed logo.svg if referenced
logo_path = Path("/home/z/my-project/public/logo.svg")
if logo_path.exists():
    with open(logo_path, "rb") as f:
        b64 = base64.b64encode(f.read()).decode()
    image_map["/logo.svg"] = f"data:image/svg+xml;base64,{b64}"

# Step 4: Replace image src and url() references with data URIs
for path, data_uri in image_map.items():
    # Replace in src="/cosmic/x.png"
    html = html.replace(f'src="{path}"', f'src="{data_uri}"')
    html = html.replace(f"src='{path}'", f"src='{data_uri}'")
    # Replace in url(/cosmic/x.png)
    html = html.replace(f"url({path})", f"url({data_uri})")
    html = html.replace(f"url('{path}')", f"url('{data_uri}')")

# Step 5: Build the final standalone HTML
# Remove the Next.js dev scripts (they won't work in a static file)
html = re.sub(r'<script[^>]*src="[^"]*/_next/[^"]*"[^>]*>\s*</script>', '', html)
# Remove the __next data script
html = re.sub(r'<script[^>]*id="__NEXT_DATA__"[^>]*>.*?</script>', '', html, flags=re.DOTALL)
# Remove inline Next.js bootstrap scripts that reference chunks
html = re.sub(r'<script>\s*self\.__next_f\.push.*?\]\)\s*</script>', '', html, flags=re.DOTALL)

# Add the globals.css as a <style> block in the head
style_tag = f"<style>\n/* === globals.css (cosmic theme) === */\n{css}\n</style>"
# Insert before </head>
html = html.replace("</head>", f"{style_tag}\n</head>", 1)

# Add a note banner (hidden, just metadata)
note = """<!-- Aethon, la Luz Primordial — Sitio web standalone autocontenido -->
<!-- Generado a partir del proyecto Next.js. Todas las imágenes embebidas en base64. -->
<!-- Las funciones interactivas (React) NO funcionan en este archivo estático; -->
<!-- es una captura visual del sitio. Para la experiencia interactiva, usa el proyecto Next.js. -->"""
html = note + "\n" + html

out_path = "/home/z/my-project/download/Aethon_Sitio_Web.html"
with open(out_path, "w", encoding="utf-8") as f:
    f.write(html)
print(f"\n✅ Standalone HTML written: {out_path}")
print(f"   Size: {len(html) / 1024:.1f} KB")
