#!/usr/bin/env python3
"""Buscador fallback para Task 49: el skill web-search (z-ai CLI) quedó en
rate-limit 429 persistente (>10 min); este script usa DuckDuckGo Lite y
guarda JSON con la misma forma (query, url, name, snippet, host_name).
Uso: python3 ddg_search.py <tag> "<query>" [num]"""
import json, re, sys, time, urllib.parse
import requests
from bs4 import BeautifulSoup

UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/124.0 Safari/537.36")
BASE = "/home/z/my-project/AethonMod/AethonMod/research/v631/busquedas"

def ddg_lite(query, num=8):
    r = requests.post("https://lite.duckduckgo.com/lite/", data={"q": query},
                      headers={"User-Agent": UA, "Content-Type": "application/x-www-form-urlencoded"},
                      timeout=25)
    r.raise_for_status()
    soup = BeautifulSoup(r.text, "html.parser")
    out, seen = [], set()
    for a in soup.find_all("a"):
        href = a.get("href", "")
        if "uddg=" not in href:
            continue
        real = urllib.parse.unquote(href.split("uddg=")[1].split("&")[0])
        if real in seen or real.startswith("https://lite.duckduckgo.com"):
            continue
        seen.add(real)
        snip = ""
        td = a.find_parent("td")
        if td:
            nxt = td.find_next_sibling("td")
            if nxt and nxt.find("span", class_="result-snippet"):
                snip = nxt.find("span", class_="result-snippet").get_text(" ", strip=True)
            elif nxt and "result-snippet" in str(nxt.get("class", [])):
                snip = nxt.get_text(" ", strip=True)
        if not snip:
            tr = a.find_parent("tr")
            if tr:
                for sib in tr.find_next_siblings("tr", limit=2):
                    sp = sib.find("span", class_="result-snippet")
                    if sp:
                        snip = sp.get_text(" ", strip=True)
                        break
        out.append({"url": real, "name": a.get_text(" ", strip=True), "snippet": snip,
                    "host_name": re.sub(r"^https?://", "", real).split("/")[0]})
        if len(out) >= num:
            break
    return out

if __name__ == "__main__":
    tag, query = sys.argv[1], sys.argv[2]
    num = int(sys.argv[3]) if len(sys.argv) > 3 else 8
    res = {"query": query, "engine": "ddg-lite", "results": ddg_lite(query, num)}
    with open(f"{BASE}/{tag}.json", "w", encoding="utf-8") as f:
        json.dump(res, f, ensure_ascii=False, indent=1)
    print(f"[{tag}] {len(res['results'])} resultados")
    for it in res["results"]:
        print(f"  - {it['name'][:88]} | {it['url'][:96]}")
