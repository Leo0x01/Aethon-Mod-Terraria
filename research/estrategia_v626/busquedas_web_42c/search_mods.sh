#!/bin/bash
declare -a QUERIES=("Wikszilla" "YetAnotherWeapon" "Orchid Mod" "Clicker Class" "SacredTools" "Ancients Awakened" "Elements Awoken" "Polarities" "Aequus" "Verdant" "Shadows of Abaddon" "Coralite" "Stellamod" "Lunar Veil" "Stars Above" "Ammulet of Many Minions" "Summoners Association" "Reduced Grinding" "Dragon Lens" "Veinminer" "Better Rain" "Torch God" "Wells" "Fargo's Souls" "Calamity Community Remix" "Subworld Library" "Lucy's QoL" "Terraria Ambience" "Wisdom" "Potency")
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
    if (out.length >= 6) break;
  }
  return JSON.stringify(out);
})()" 2>/dev/null | head -c 2000)
  echo "QUERY[$q] => $res"
done
