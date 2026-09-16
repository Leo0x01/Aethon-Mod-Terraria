#!/usr/bin/env python3
"""Fetcher de páginas para Task 49 (page_reader en 429). Extrae texto limpio.
Uso: python3 lee.py <slug> "<url>" [--raw]"""
import json, re, sys, time
import requests
from bs4 import BeautifulSoup

UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/124.0 Safari/537.36")
BASE = "/home/z/my-project/AethonMod/AethonMod/research/v631/search_results/t49/pages"

def fetch(url):
    # wiki.gg/fandom: la API MediaWiki suele pasar donde el HTML da 403
    m = re.match(r"(https?://[^/]+)/wiki/(.+)", url)
    if m and any(d in m.group(1) for d in ("wiki.gg", "fandom.com")):
        api = (f"{m.group(1)}/api.php?action=parse&page={m.group(2)}"
               f"&format=json&prop=wikitext&redirects=1")
        try:
            r = requests.get(api, headers={"User-Agent": UA}, timeout=30)
            if r.ok:
                j = r.json()
                wt = j.get("parse", {}).get("wikitext", {}).get("*")
                if wt:
                    return wt
        except Exception:
            pass
    r = requests.get(url, headers={"User-Agent": UA, "Accept-Language": "en-US,en;q=0.9,es;q=0.8,zh-CN;q=0.7"},
                     timeout=30)
    r.raise_for_status()
    return r.text

def extract(html):
    soup = BeautifulSoup(html, "html.parser")
    for t in soup(["script", "style", "nav", "footer", "header", "aside"]):
        t.decompose()
    # contenido principal de wikis
    main = (soup.select_one("#mw-content-text") or soup.select_one("article")
            or soup.select_one("main") or soup.body or soup)
    # infoboxes de wiki.gg primero (datos duros)
    lines = []
    for box in main.select(".infobox, .wikitable"):
        lines.append("[TABLA] " + box.get_text(" | ", strip=True)[:1500])
    txt = main.get_text("\n", strip=True)
    txt = re.sub(r"\n{3,}", "\n\n", txt)
    return "\n".join(lines) + "\n\n" + txt

if __name__ == "__main__":
    slug, url = sys.argv[1], sys.argv[2]
    ok = False
    for i in range(3):
        try:
            html = fetch(url)
            if len(html) < 400 and i < 2:
                time.sleep(3); continue
            text = html if "--raw" in sys.argv else extract(html)
            with open(f"{BASE}/{slug}.txt", "w", encoding="utf-8") as f:
                f.write(f"URL: {url}\n=====\n{text}")
            print(f"[{slug}] ok {len(text)} chars")
            ok = True
            break
        except Exception as e:
            print(f"[{slug}] intento {i+1} falló: {e}")
            time.sleep(4)
    if not ok:
        print(f"[{slug}] FALLO TOTAL")
