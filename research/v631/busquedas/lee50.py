#!/usr/bin/env python3
"""Lector de páginas para Task 50 (fallback cuando z-ai page_reader va a 429).
Intenta primero el CLI z-ai (skill web-reader); si falla, requests+BS4.
Uso: python3 lee50.py <tag> <url>"""
import json, subprocess, sys, re
import requests
from bs4 import BeautifulSoup

BASE = "/home/z/my-project/AethonMod/AethonMod/research/v631/busquedas/t50"
UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/124.0 Safari/537.36")

def zai(url):
    try:
        p = subprocess.run(["z-ai", "function", "-n", "page_reader", "-a",
                            json.dumps({"url": url})],
                           capture_output=True, text=True, timeout=60)
        if p.returncode == 0 and p.stdout.strip():
            d = json.loads(p.stdout)
            data = d.get("data", d)
            if data.get("html") or data.get("text"):
                return {"engine": "z-ai", "title": data.get("title", ""),
                        "text": data.get("text") or data.get("html", "")}
    except Exception:
        pass
    return None

def fetch(url):
    r = requests.get(url, headers={"User-Agent": UA,
                                   "Accept-Language": "en-US,en;q=0.9,zh-CN;q=0.8"},
                     timeout=30)
    r.raise_for_status()
    soup = BeautifulSoup(r.text, "html.parser")
    for t in soup(["script", "style", "noscript"]):
        t.decompose()
    title = soup.title.get_text(" ", strip=True) if soup.title else ""
    text = soup.get_text("\n")
    text = re.sub(r"\n{3,}", "\n\n", text)
    return {"engine": "requests", "title": title, "text": text}

if __name__ == "__main__":
    tag, url = sys.argv[1], sys.argv[2]
    out = zai(url) or fetch(url)
    with open(f"{BASE}/{tag}.json", "w", encoding="utf-8") as f:
        json.dump({"url": url, **out}, f, ensure_ascii=False, indent=1)
    print(f"[{tag}] motor={out['engine']} titulo={out['title'][:80]} chars={len(out['text'])}")
