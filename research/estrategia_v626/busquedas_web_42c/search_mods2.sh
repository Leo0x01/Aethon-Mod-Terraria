#!/bin/bash
declare -a QUERIES=("Wikszilla Weapon" "Reduced Grinding" "Dragon Lens" "Fargo Souls DLC" "SOTS" "Spirit Mod" "Exxo Avalon" "Tremor" "GRealm" "SacredTools SHA" "W1K's" "Enigma" "LuiAFK" "Wells Fargo" "Bloom" "Nights of the Moon" "Noxium" "Osmo" "Bag of Many Things" "Start With Base" "Terra Kitchen" "Metanoia" "Arcania" "Split" "Sudden Deaths" "Boss Rush" "Wrath of the Gods")
for q in "${QUERIES[@]}"; do
  enc=$(python3 -c "import urllib.parse,sys; print(urllib.parse.quote(sys.argv[1]))" "$q")
  agent-browser open "https://steamcommunity.com/workshop/browse/?appid=1281930&searchtext=$enc&browsesort=textsearch&section=readytouseitems" >/dev/null 2>&1
  agent-browser wait --load networkidle >/dev/null 2>&1
  agent-browser wait 1800 >/dev/null 2>&1
  res=$(agent-browser eval "
(() => {
  const links = [...document.querySelectorAll('a[href*=\"sharedfiles/filedetails\"]')];
  const seen = new Set(); const out = [];
  for (const a of links) {
    const id = (a.href.match(/id=(\d+)/)||[])[1];
    if (!id || seen.has(id)) continue; seen.add(id);
    const img = a.querySelector('img');
    out.push({id: id, t: img ? img.alt : (a.textContent||'').trim().slice(0,90)});
    if (out.length >= 5) break;
  }
  return JSON.stringify(out);
})()" 2>/dev/null | head -c 1500)
  echo "QUERY[$q] => $res"
done
