#!/usr/bin/env python3
"""Buscador combinado para Task 49. El skill web-search (z-ai CLI) lleva
~20 min en 429/477; este script encadena: DDG-lite (GET) -> DDG-html (POST)
-> Bing (con decode de redirects ck/a). Guarda JSON por tag.
Uso: python3 busca.py <tag> "<query>" [num]"""
import base64, json, re, subprocess, sys, time, urllib.parse
import requests
from bs4 import BeautifulSoup

UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/124.0 Safari/537.36")
BASE = "/home/z/my-project/AethonMod/AethonMod/research/v631/busquedas"

def _snip_after(soup_el, cls="result-snippet"):
    tr = soup_el.find_parent("tr")
    if tr:
        for sib in tr.find_next_siblings("tr", limit=3):
            sp = sib.find("span", class_=cls)
            if sp:
                return sp.get_text(" ", strip=True)
    return ""

def ddg_lite(query, num):
    r = requests.get("https://lite.duckduckgo.com/lite/", params={"q": query},
                     headers={"User-Agent": UA}, timeout=20)
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
        out.append({"url": real, "name": a.get_text(" ", strip=True),
                    "snippet": _snip_after(a), "host_name": re.sub(r"^https?://", "", real).split("/")[0]})
        if len(out) >= num:
            break
    return out

def ddg_html(query, num):
    r = requests.post("https://html.duckduckgo.com/html/", data={"q": query},
                      headers={"User-Agent": UA}, timeout=20)
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
    r = requests.get("https://www.bing.com/search", params={"q": query, "count": num, "mkt": "en-US"},
                     headers={"User-Agent": UA, "Accept-Language": "en-US,en;q=0.9"},
                     cookies={"SRCHHPGUSR": "SRCHLANG=en"}, timeout=20)
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

def zai(query, num):
    try:
        p = subprocess.run(["z-ai", "function", "-n", "web_search", "-a",
                            json.dumps({"query": query, "num": num})],
                           capture_output=True, text=True, timeout=45)
        if p.returncode == 0 and p.stdout.strip():
            return json.loads(p.stdout), "z-ai"
    except Exception:
        pass
    return None, None

if __name__ == "__main__":
    tag, query = sys.argv[1], sys.argv[2]
    num = int(sys.argv[3]) if len(sys.argv) > 3 else 8

    res, engine = zai(query, num)
    if not res:
        for fn, name in ((ddg_lite, "ddg-lite"), (ddg_html, "ddg-html"), (bing, "bing")):
            for intento in range(2):
                try:
                    res = fn(query, num)
                    if res:
                        engine = name
                        break
                except Exception as e:
                    time.sleep(4)
            if res:
                break
            time.sleep(3)
    if not res:
        res = []
        engine = "ninguno (429/timeout)"

    data = {"query": query, "engine": engine, "results": res}
    with open(f"{BASE}/{tag}.json", "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    print(f"[{tag}] motor={engine} resultados={len(res)}")
    for it in res:
        print(f"  - {it['name'][:88]} | {it['url'][:96]}")
