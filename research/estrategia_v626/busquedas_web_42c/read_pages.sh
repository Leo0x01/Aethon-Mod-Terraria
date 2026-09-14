#!/bin/bash
declare -a PAGES=(
"2824688072:calamity"
"2909886416:thorium"
"2563862309:stars_above"
"2838015851:catalyst"
"3019925104:lunar_veil"
"2906178094:everglow"
"2811803870:overhaul"
"2815540735:fargos_souls"
"2563309347:magic_storage"
"2669644269:boss_checklist"
"2995193002:wotg"
"2984622072:starlight_river"
"2893332653:redemption"
"2858396998:meac"
"2926797349:coralite"
"2843112914:secrets_shadows"
)
for entry in "${PAGES[@]}"; do
  id="${entry%%:*}"; name="${entry#*:}"
  agent-browser open "https://steamcommunity.com/sharedfiles/filedetails/?id=$id" >/dev/null 2>&1
  agent-browser wait --load networkidle >/dev/null 2>&1
  agent-browser wait 2500 >/dev/null 2>&1
  agent-browser eval "
(() => {
  const el = document.querySelector('.workshopItemDescription') || document.querySelector('[class*=escription]');
  const stats = document.querySelector('.detailsStatsContainerRight');
  const tags = [...document.querySelectorAll('.appTag')].map(t=>t.textContent.trim()).slice(0,10);
  const title = document.querySelector('.workshopItemTitle');
  return JSON.stringify({title: title?title.textContent.trim():'', desc: el ? el.innerText.slice(0,4500) : 'NOTFOUND', stats: stats?stats.innerText.replace(/\\n/g,' | ').slice(0,300):'', tags: tags.join(',')});
})()" 2>/dev/null > "desc_${name}.json"
  echo "saved desc_${name}.json ($(wc -c < desc_${name}.json) bytes)"
done
