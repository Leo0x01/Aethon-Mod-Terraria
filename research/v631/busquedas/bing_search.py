#!/usr/bin/env python3
"""Bing search fallback para Task 49 (z-ai web_search en rate-limit 429).
Guarda resultados como JSON en esta carpeta, con el mismo espíritu del
skill web-search (query, num, resultados con url/name/snippet/host)."""
import json, re, sys, time, html
import requests
from bs4 import BeautifulSoup

UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/124.0 Safari/537.36")

def bing_search(query, num=8):
    url = "https://www.bing.com/search"
    params = {"q": query, "count": num, "setlang": "en"}
    r = requests.get(url, params=params, headers={"User-Agent": UA}, timeout=20)
    r.raise_for_status()
    soup = BeautifulSoup(r.text, "html.parser")
    out, seen = [], set()
    for li in soup.select("li.b_algo"):
        a = li.select_one("h2 a")
        if not a or not a.get("href"):
            continue
        href = a["href"]
        if href in seen:
            continue
        seen.add(href)
        p = li.select_one(".b_caption p, p")
        snip = p.get_text(" ", strip=True) if p else ""
        out.append({
            "url": href,
            "name": a.get_text(" ", strip=True),
            "snippet": html.unescape(snip),
            "host_name": re.sub(r"^https?://", "", href).split("/")[0],
        })
        if len(out) >= num:
            break
    return out

if __name__ == "__main__":
    tag, query, num = sys.argv[1], sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 8
    res = {"query": query, "engine": "bing-curl", "results": bing_search(query, num)}
    path = f"/home/z/my-project/AethonMod/AethonMod/research/v631/busquedas/{tag}.json"
    with open(path, "w", encoding="utf-8") as f:
        json.dump(res, f, ensure_ascii=False, indent=1)
    print(f"[{tag}] {len(res['results'])} resultados -> {path}")
    for it in res["results"]:
        print(f"  - {it['name'][:90]} | {it['url'][:100]}")
