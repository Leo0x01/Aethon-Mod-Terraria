#!/usr/bin/env python3
"""Buscador rápido (z-ai 429 confirmado): DDG-lite -> DDG-html -> Bing en
paralelo para Task 49 (continuación). Guarda JSON por tag.
Uso: python3 busca2.py <tag1>="<query1>" <tag2>="<query2>" ..."""
import base64, concurrent.futures as cf, json, re, subprocess, sys, time
import urllib.parse
import requests
from bs4 import BeautifulSoup

UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/124.0 Safari/537.36")
BASE = "/home/z/my-project/AethonMod/AethonMod/research/v631/busquedas"
NUM = 8

def ddg_lite(query, num):
    r = requests.get("https://lite.duckduckgo.com/lite/", params={"q": query},
                     headers={"User-Agent": UA}, timeout=12)
    soup = BeautifulSoup(r.text, "html.parser")
    out, seen = [], set()
    for a in soup.find_all("a"):
        href = a.get("href", "")
        if "uddg=" not in href:
            continue
        real = urllib.parse.unquote(href.split("uddg=")[1].split("&")[0])
        if real in seen or "duckduckgo.com" in real:
            continue
        seen.add(real)
        out.append({"url": real, "name": a.get_text(" ", strip=True), "snippet": "",
                    "host_name": re.sub(r"^https?://", "", real).split("/")[0]})
        if len(out) >= num:
            break
    return out

def ddg_html(query, num):
    r = requests.post("https://html.duckduckgo.com/html/", data={"q": query},
                      headers={"User-Agent": UA}, timeout=12)
    soup = BeautifulSoup(r.text, "html.parser")
    out, seen = [], set()
    for res in soup.select("div.result"):
        a = res.select_one("a.result__a")
        if not a:
            continue
        href = a.get("href", "")
        if "uddg=" in href:
            real = urllib.parse.unquote(href.split("uddg=")[1].split("&")[0])
        elif href.startswith("http"):
            real = href
        else:
            continue
        if real in seen:
            continue
        seen.add(real)
        sp = res.select_one(".result__snippet")
        out.append({"url": real, "name": a.get_text(" ", strip=True),
                    "snippet": sp.get_text(" ", strip=True) if sp else "",
                    "host_name": re.sub(r"^https?://", "", real).split("/")[0]})
        if len(out) >= num:
            break
    return out

def _bing_decode(href):
    m = re.search(r"[?&]u=a1([^&]+)", href)
    if m:
        b = m.group(1)
        b += "=" * (-len(b) % 4)
        try:
            return base64.urlsafe_b64decode(b).decode("utf-8", "ignore")
        except Exception:
            return href
    return href

def bing(query, num):
    r = requests.get("https://www.bing.com/search", params={"q": query, "count": str(num), "mkt": "en-US"},
                     headers={"User-Agent": UA, "Accept-Language": "en-US,en;q=0.9"},
                     cookies={"SRCHHPGUSR": "SRCHLANG=en"}, timeout=12)
    soup = BeautifulSoup(r.text, "html.parser")
    out, seen = [], set()
    for li in soup.select("li.b_algo"):
        a = li.select_one("h2 a")
        if not a or not a.get("href"):
            continue
        real = _bing_decode(a["href"])
        if real in seen or "bing.com" in real:
            continue
        seen.add(real)
        p = li.select_one(".b_caption p, p")
        out.append({"url": real, "name": a.get_text(" ", strip=True),
                    "snippet": p.get_text(" ", strip=True) if p else "",
                    "host_name": re.sub(r"^https?://", "", real).split("/")[0]})
        if len(out) >= num:
            break
    return out

def run_one(tag, query):
    for fn, name in ((ddg_lite, "ddg-lite"), (ddg_html, "ddg-html"), (bing, "bing")):
        for intento in range(2):
            try:
                res = fn(query, NUM)
                if res:
                    with open(f"{BASE}/{tag}.json", "w", encoding="utf-8") as f:
                        json.dump({"query": query, "engine": name, "results": res},
                                  f, ensure_ascii=False, indent=1)
                    return tag, name, len(res)
            except Exception:
                time.sleep(2)
        time.sleep(1.5)
    with open(f"{BASE}/{tag}.json", "w", encoding="utf-8") as f:
        json.dump({"query": query, "engine": "ninguno", "results": []},
                  f, ensure_ascii=False, indent=1)
    return tag, "ninguno", 0

if __name__ == "__main__":
    jobs = []
    for arg in sys.argv[1:]:
        tag, q = arg.split("=", 1)
        jobs.append((tag, q))
    with cf.ThreadPoolExecutor(max_workers=4) as ex:
        futs = [ex.submit(run_one, t, q) for t, q in jobs]
        for fu in cf.as_completed(futs):
            print(fu.result())
