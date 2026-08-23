#!/usr/bin/env python3
"""Genera un PDF profesional del documento de diseño del mod Aethon."""
import re
from pathlib import Path

# Read combined markdown
with open("/tmp/combined-doc.md", "r") as f:
    md = f.read()

def md_to_html(text):
    out = []
    lines = text.split("\n")
    i = 0
    while i < len(lines):
        line = lines[i]
        m = re.match(r"^(#{1,4})\s+(.*)", line)
        if m:
            level = len(m.group(1))
            content = inline(m.group(2))
            out.append(f"<h{level}>{content}</h{level}>")
            i += 1
            continue
        if re.match(r"^---+\s*$", line):
            out.append("<hr/>")
            i += 1
            continue
        if line.startswith("> "):
            quote_lines = []
            while i < len(lines) and lines[i].startswith("> "):
                quote_lines.append(inline(lines[i][2:]))
                i += 1
            out.append(f"<blockquote>{' '.join(quote_lines)}</blockquote>")
            continue
        if "|" in line and i + 1 < len(lines) and re.match(r"^\|[\s:|-]+\|\s*$", lines[i+1]):
            header = [c.strip() for c in line.strip("|").split("|")]
            i += 2
            rows = []
            while i < len(lines) and "|" in lines[i] and lines[i].strip():
                rows.append([c.strip() for c in lines[i].strip("|").split("|")])
                i += 1
            html = '<table><thead><tr>' + ''.join(f'<th>{inline(h)}</th>' for h in header) + '</tr></thead><tbody>'
            for row in rows:
                html += '<tr>' + ''.join(f'<td>{inline(c)}</td>' for c in row) + '</tr>'
            html += '</tbody></table>'
            out.append(html)
            continue
        if re.match(r"^\s*[-*]\s", line):
            items = []
            while i < len(lines) and re.match(r"^\s*[-*]\s", lines[i]):
                items.append(f"<li>{inline(re.sub(r'^\s*[-*]\s','',lines[i]))}</li>")
                i += 1
            out.append(f"<ul>{''.join(items)}</ul>")
            continue
        if re.match(r"^\s*\d+\.\s", line):
            items = []
            while i < len(lines) and re.match(r"^\s*\d+\.\s", lines[i]):
                items.append(f"<li>{inline(re.sub(r'^\s*\d+\.\s','',lines[i]))}</li>")
                i += 1
            out.append(f"<ol>{''.join(items)}</ol>")
            continue
        if not line.strip():
            i += 1
            continue
        para_lines = []
        while i < len(lines) and lines[i].strip() and not lines[i].startswith("#") and not lines[i].startswith("|") and not re.match(r"^\s*[-*]\s", lines[i]) and not re.match(r"^\s*\d+\.\s", lines[i]) and not lines[i].startswith("> ") and not re.match(r"^---+\s*$", lines[i]):
            para_lines.append(lines[i])
            i += 1
        if para_lines:
            out.append(f"<p>{inline(' '.join(para_lines))}</p>")
    return '\n'.join(out)

def inline(text):
    text = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"(?<!\*)\*([^*]+?)\*(?!\*)", r"<em>\1</em>", text)
    text = re.sub(r"`([^`]+)`", r"<code>\1</code>", text)
    return text

body_html = md_to_html(md)

html_doc = f"""<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8">
<title>Aethon, la Luz Primordial — Documento de Diseño del Mod</title>
<style>
@page {{
  size: A4;
  margin: 22mm 18mm 22mm 18mm;
}}
* {{ box-sizing: border-box; }}
html, body {{
  margin: 0;
  padding: 0;
  background: #0d0a1a;
  color: #e8e4f0;
  font-family: "Noto Sans", "DejaVu Sans", "Helvetica Neue", Arial, sans-serif;
  font-size: 10.5pt;
  line-height: 1.6;
  -webkit-print-color-adjust: exact;
  print-color-adjust: exact;
}}
h1, h2, h3, h4 {{
  color: #f5c451;
  font-weight: 700;
  line-height: 1.3;
  margin-top: 1.4em;
  margin-bottom: 0.5em;
  break-after: avoid;
}}
h1 {{
  font-size: 22pt;
  border-bottom: 2px solid #f5c451;
  padding-bottom: 0.3em;
  margin-top: 0.6em;
}}
h2 {{
  font-size: 16pt;
  color: #b388ff;
  border-left: 4px solid #b388ff;
  padding-left: 0.5em;
}}
h3 {{
  font-size: 13pt;
  color: #ff9a3c;
}}
h4 {{
  font-size: 11pt;
  color: #3dd6c4;
}}
p {{ margin: 0.5em 0; text-align: justify; }}
strong {{ color: #fff; }}
em {{ color: #c9b8e6; }}
code {{
  background: rgba(179,136,255,0.15);
  color: #d4b8ff;
  padding: 1px 5px;
  border-radius: 3px;
  font-family: "JetBrains Mono","DejaVu Sans Mono", monospace;
  font-size: 9pt;
}}
blockquote {{
  border-left: 3px solid #f5c451;
  background: rgba(245,196,81,0.08);
  padding: 0.6em 1em;
  margin: 0.8em 0;
  border-radius: 0 6px 6px 0;
  color: #d4c8a0;
}}
hr {{
  border: none;
  border-top: 1px solid rgba(245,196,81,0.3);
  margin: 1.5em 0;
}}
ul, ol {{ margin: 0.5em 0; padding-left: 1.5em; }}
li {{ margin: 0.25em 0; }}
table {{
  width: 100%;
  border-collapse: collapse;
  margin: 1em 0;
  font-size: 9.5pt;
  break-inside: avoid;
}}
th, td {{
  border: 1px solid rgba(179,136,255,0.3);
  padding: 6px 9px;
  text-align: left;
  vertical-align: top;
}}
thead th {{
  background: rgba(245,196,81,0.15);
  color: #f5c451;
  font-weight: 700;
}}
tbody tr:nth-child(even) {{
  background: rgba(179,136,255,0.06);
}}
.cover {{
  page-break-after: always;
  height: 100vh;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  text-align: center;
  background: radial-gradient(ellipse at center, #1a1030 0%, #0d0a1a 70%);
  padding: 0 20mm;
}}
.cover .sigil {{ font-size: 60pt; color: #f5c451; margin-bottom: 0.2em; text-shadow: 0 0 30px rgba(245,196,81,0.6); }}
.cover h1 {{ border: none; font-size: 30pt; margin: 0.2em 0; }}
.cover .subtitle {{ color: #b388ff; font-size: 14pt; margin: 0.5em 0 2em; }}
.cover .meta {{ color: #7a6b9a; font-size: 10pt; font-family: monospace; }}
.cover .tags {{ margin-top: 2em; }}
.cover .tag {{ display: inline-block; background: rgba(245,196,81,0.1); color: #f5c451; border: 1px solid rgba(245,196,81,0.3); padding: 4px 12px; border-radius: 999px; font-size: 9pt; margin: 3px; }}
</style>
</head>
<body>

<div class="cover">
  <div class="sigil">✦</div>
  <h1>Aethon, la Luz Primordial</h1>
  <div class="subtitle">Documento de Diseño del Mod + Roadmap de Implementación</div>
  <div class="tags">
    <span class="tag">Terraria · tModLoader</span>
    <span class="tag">3 Ramas Principales</span>
    <span class="tag">Niveles Infinitos</span>
    <span class="tag">Árboles Procedurales</span>
    <span class="tag">Absorción de Lore</span>
  </div>
  <div class="meta">Concepto de mod · v2.0 · Idioma: Español</div>
</div>

{body_html}

</body>
</html>
"""

out_html = "/home/z/my-project/download/aethon-diseno.html"
with open(out_html, "w") as f:
    f.write(html_doc)
print(f"HTML written: {out_html} ({len(html_doc)} bytes)")
