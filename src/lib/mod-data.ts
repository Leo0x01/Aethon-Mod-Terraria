// Central data module for the "Aethon, the Primordial Light" Terraria mod showcase.

export type WeaponId = "bow" | "sword" | "cannon" | "book";

export type NodeRarity = "common" | "rare" | "legendary";

export interface SkillNode {
  id: string;
  name: string;
  branch: string; // branch index/label
  rarity: NodeRarity;
  cost: number; // skill point cost
  effect: string;
  prereq?: string; // node id that must be unlocked first
  // procedural placement (relative 0..1 coords on the constellation canvas)
  x: number;
  y: number;
}

export interface SkillBranch {
  id: string;
  name: string;
  icon: string; // emoji glyph
  color: string; // tailwind-ish hue token
  description: string;
  capstone?: boolean;
}

export interface Weapon {
  id: WeaponId;
  name: string;
  epithet: string;
  classLabel: string;
  fantasy: string;
  image: string;
  accent: string; // hex accent for glows
  accentSoft: string;
  stats: { label: string; value: string }[];
  baseProjectiles: string[];
  branches: SkillBranch[];
  skillTree: SkillNode[];
  loreAbsorption: string;
}

export interface Boss {
  id: string;
  name: string;
  tier: "mini" | "echo" | "cosmic" | "final";
  phaseLabel?: string;
  hp: string;
  description: string;
  mechanics: string[];
  unlock: string;
  accent: string;
}

export interface LoreEntry {
  id: string;
  title: string;
  era: string;
  body: string;
}

// ---------------------------------------------------------------------------
// Skill point schedule
// ---------------------------------------------------------------------------

export interface SkillTier {
  min: number;
  max: number;
  perLevel: number;
}

export const SKILL_TIERS: SkillTier[] = [
  { min: 1, max: 10, perLevel: 1 },
  { min: 11, max: 20, perLevel: 2 },
  { min: 21, max: 30, perLevel: 3 },
  { min: 31, max: 40, perLevel: 4 },
  { min: 41, max: 50, perLevel: 5 },
  { min: 51, max: 60, perLevel: 6 },
  { min: 61, max: 70, perLevel: 7 },
  { min: 71, max: 80, perLevel: 8 },
  { min: 81, max: 90, perLevel: 9 },
  { min: 91, max: 100, perLevel: 10 },
  { min: 101, max: Infinity, perLevel: 10 },
];

/** Total skill points earned once the weapon reaches `level` (1-indexed). */
export function cumulativeSkillPoints(level: number): number {
  if (level <= 0) return 0;
  let total = 0;
  for (const tier of SKILL_TIERS) {
    const upper = Math.min(level, tier.max);
    if (upper >= tier.min) {
      total += (upper - tier.min + 1) * tier.perLevel;
    }
    if (level <= tier.max) break;
  }
  return total;
}

/** Skill points gained specifically at this level. */
export function pointsForLevel(level: number): number {
  for (const tier of SKILL_TIERS) {
    if (level >= tier.min && level <= tier.max) return tier.perLevel;
  }
  return 10; // post-100 flat
}

/** XP required to go FROM `level` TO `level+1`. */
export function xpForNextLevel(level: number): number {
  return Math.floor(80 * Math.pow(level, 1.5));
}

/** Cumulative XP needed to REACH `level` (starting from level 1). */
export function cumulativeXp(level: number): number {
  let total = 0;
  for (let l = 1; l < level; l++) total += xpForNextLevel(l);
  return total;
}

export interface Milestone {
  level: number;
  label: string;
  desc: string;
}

export const MILESTONES: Milestone[] = [
  { level: 10, label: "First Awakening", desc: "Shard solidifies — gains a runic glyph." },
  { level: 25, label: "Starlight Rain", desc: "Cosmic event: ambient meteor shower, rare ore." },
  { level: 50, label: "The Hollowing", desc: "Hollow Sanctum spreads, new mobs emerge." },
  { level: 75, label: "Dimensional Rifts", desc: "Mini-dungeons spawn with loot." },
  { level: 100, label: "Aethon's Stirring", desc: "Echoes of past wielders appear as bosses." },
  { level: 150, label: "The Awakening", desc: "Final boss Aethon becomes available." },
  { level: 200, label: "Ascended Form", desc: "Shard takes its true galactic shape." },
];

// ---------------------------------------------------------------------------
// Weapons
// ---------------------------------------------------------------------------

// Helper to build constellation nodes on a hex grid per branch.
function buildTree(
  branchDefs: { name: string; nodes: { n: string; e: string; r?: NodeRarity; c?: number }[] }[],
): SkillNode[] {
  const nodes: SkillNode[] = [];
  const branchCount = branchDefs.length;
  branchDefs.forEach((branch, bi) => {
    const angle = (bi / branchCount) * Math.PI * 2 - Math.PI / 2;
    const baseR = 0.18;
    branch.nodes.forEach((node, ni) => {
      const r = baseR + ni * 0.13;
      const jitter = (bi % 2 === 0 ? 0.04 : -0.04);
      const x = 0.5 + Math.cos(angle + jitter * ni) * r * 1.15;
      const y = 0.5 + Math.sin(angle + jitter * ni) * r * 1.0;
      nodes.push({
        id: `${branch.name}-${ni}`,
        name: node.n,
        branch: branch.name,
        rarity: node.r ?? "common",
        cost: node.c ?? 1,
        effect: node.e,
        prereq: ni > 0 ? `${branch.name}-${ni - 1}` : undefined,
        x,
        y,
      });
    });
  });
  return nodes;
}

export const WEAPONS: Weapon[] = [
  {
    id: "bow",
    name: "Lumina",
    epithet: "the Starbowed",
    classLabel: "Ranged",
    fantasy: "Arrows woven from starlight that seek their mark across the void.",
    image: "/cosmic/weapon-bow.png",
    accent: "#f5c451",
    accentSoft: "rgba(245,196,81,0.15)",
    stats: [
      { label: "Damage", value: "Scaling (Lv × 2.4)" },
      { label: "Draw speed", value: "12 → 4 frames" },
      { label: "Ammo", value: "Light arrows (free)" },
      { label: "Knockback", value: "Moderate" },
    ],
    baseProjectiles: ["Starlight Arrow", "Piercing Beam", "Homing Mote"],
    branches: [
      { id: "arrow", name: "Arrow Genesis", icon: "➶", color: "amber", description: "Forge new arrow types from pure light." },
      { id: "quiver", name: "Quiver Mastery", icon: "🎒", color: "amber", description: "More arrows per shot, faster draws." },
      { id: "mark", name: "Hunter's Mark", icon: "◎", color: "amber", description: "Tag, track, and crit-stack enemies." },
      { id: "celestial", name: "Celestial Shots", icon: "✦", color: "amber", description: "Meteor Volley, Solar Flare, Eclipse." },
      { id: "phantom", name: "Phantom Quiver", icon: "◈", color: "amber", description: "Ethereal arrows that ignore terrain." },
      { id: "absorb", name: "Lore Absorption", icon: "⭐", color: "amber", description: "Absorb every bow's signature behavior.", capstone: true },
    ],
    skillTree: buildTree([
      { name: "arrow", nodes: [
        { n: "Light Arrow", e: "Base arrow becomes pure light; ignores 5 defense." },
        { n: "Void Arrow", e: "Alternate arrow type: drains 4 mana, deals +30% damage.", r: "rare" },
        { n: "Split Shot", e: "Fires 2 arrows in a fan.", r: "rare" },
        { n: "Homing Star", e: "Arrows curve toward enemies within 18 tiles.", r: "rare" },
        { n: "Trinity Volley", e: "Every 3rd shot fires 3 seeking arrows.", r: "legendary", c: 3 },
      ]},
      { name: "quiver", nodes: [
        { n: "Quick Draw", e: "Draw time -15%." },
        { n: "Twin String", e: "+1 arrow per shot.", r: "rare" },
        { n: "Endless Quiver", e: "Arrows cost no ammo and never deplete.", r: "rare" },
        { n: "Barrage", e: "Hold to fire a stream of 6 arrows.", r: "rare" },
        { n: "Starfall Storm", e: "Charged shot rains 12 arrows from the sky.", r: "legendary", c: 4 },
      ]},
      { name: "mark", nodes: [
        { n: "Tag", e: "Hits mark enemies; marked foes take +10% damage." },
        { n: "Crit Stack", e: "Each hit on a marked foe +4% crit chance (max 40%).", r: "rare" },
        { n: "Weak Spot", e: "Marked enemies show a weak point; hits there always crit.", r: "rare" },
        { n: "Hunter's Focus", e: "Standing still 1s doubles crit chance.", r: "rare" },
        { n: "Killing Mark", e: "Killing a marked foe resets all cooldowns.", r: "legendary", c: 3 },
      ]},
      { name: "celestial", nodes: [
        { n: "Meteor Volley", e: "Alt-fire: 3 meteors fall on cursor.", r: "rare" },
        { n: "Solar Flare", e: "Arrows ignite enemies for 4s (burn DoT).", r: "rare" },
        { n: "Eclipse", e: "Charged shot blinds + burns all on screen (10s cd).", r: "rare" },
        { n: "Star Cascade", e: "Every arrow spawns 2 mini-stars on hit.", r: "rare" },
        { n: "Supernova", e: "Charged arrow explodes in a 12-tile supernova.", r: "legendary", c: 5 },
      ]},
      { name: "phantom", nodes: [
        { n: "Phase Shot", e: "Arrows pass through 3 tiles of terrain.", r: "rare" },
        { n: "Ricochet", e: "Arrows bounce off walls up to 2 times.", r: "rare" },
        { n: "Bouncing Betty", e: "Arrows that miss spawn a stationary light orb mine.", r: "rare" },
        { n: "Phantom Chain", e: "Ricochets between 3 enemies.", r: "rare" },
        { n: "Ethereal Form", e: "All arrows phase terrain AND pierce 5 enemies.", r: "legendary", c: 4 },
      ]},
      { name: "absorb", nodes: [
        { n: "Memory Codex I", e: "Unlock Memory Rune slot 1.", r: "rare" },
        { n: "Memory Codex II", e: "Unlock Memory Rune slot 2.", r: "rare" },
        { n: "Memory Codex III", e: "Unlock Memory Rune slot 3.", r: "rare" },
        { n: "Resonance Tuning", e: "Memorize any bow weapon's signature arrow type.", r: "legendary", c: 5 },
        { n: "Omniscient Quiver", e: "Equip 5 Memory Runes simultaneously; fuse two into one.", r: "legendary", c: 8 },
      ]},
    ]),
    loreAbsorption:
      "Lumina's capstone branch opens the Memory Codex: a ledger of every bow in Terraria (and loaded mods). Invest Resonance Shards to memorize a bow's signature arrow — then equip it as a Memory Rune alongside your own. The Starbowed becomes every bow at once.",
  },
  {
    id: "sword",
    name: "Solbrand",
    epithet: "Edge of Dawn",
    classLabel: "Melee",
    fantasy: "A blade of condensed dawn that cuts through the fabric of reality.",
    image: "/cosmic/weapon-sword.png",
    accent: "#ff9a3c",
    accentSoft: "rgba(255,154,60,0.15)",
    stats: [
      { label: "Damage", value: "Scaling (Lv × 3.1)" },
      { label: "Use time", value: "18 → 9 frames" },
      { label: "Range", value: "Melee + beam (up to 14 tiles)" },
      { label: "Knockback", value: "Strong" },
    ],
    baseProjectiles: ["Dawn Slash", "Sun Beam", "Corona Pulse"],
    branches: [
      { id: "blade", name: "Blade Genesis", icon: "⚔", color: "orange", description: "New slash types: wave, beam, whirl." },
      { id: "combo", name: "Combo Mastery", icon: "🔥", color: "orange", description: "Escalating combo meter & finishers." },
      { id: "solar", name: "Solar Wrath", icon: "☀", color: "orange", description: "Sunflare, Eclipse Cleave, Corona aura." },
      { id: "aegis", name: "Aegis of Dawn", icon: "🛡", color: "orange", description: "Parry frames, reflection, invuln dashes." },
      { id: "weight", name: "Weight of Stars", icon: "★", color: "orange", description: "Heavy hits, shockwaves, knockback." },
      { id: "absorb", name: "Lore Absorption", icon: "⭐", color: "orange", description: "Absorb every sword's signature swing.", capstone: true },
    ],
    skillTree: buildTree([
      { name: "blade", nodes: [
        { n: "Slash Wave", e: "Each swing emits a forward wave (6 tiles)." },
        { n: "Beam Slash", e: "Charged swing fires a 12-tile beam.", r: "rare" },
        { n: "Whirl", e: "Alt-fire: spin slash hitting all around.", r: "rare" },
        { n: "Thrust Combo", e: "3-hit combo ends in a piercing thrust.", r: "rare" },
        { n: "Reality Cleave", e: "Charged cleave cuts terrain & enemies alike.", r: "legendary", c: 3 },
      ]},
      { name: "combo", nodes: [
        { n: "Combo Meter", e: "Consecutive hits build a combo (max 10)." },
        { n: "Finisher", e: "At combo 10, next hit deals +200% damage.", r: "rare" },
        { n: "Momentum", e: "Combo doesn't decay while moving.", r: "rare" },
        { n: "Parry-Riposte", e: "Block + counter resets combo & adds 5.", r: "rare" },
        { n: "Infinite Edge", e: "Combo cap removed; every hit beyond 10 adds +5% damage.", r: "legendary", c: 4 },
      ]},
      { name: "solar", nodes: [
        { n: "Sunflare Slash", e: "Slashes burn enemies (blind + DoT).", r: "rare" },
        { n: "Eclipse Cleave", e: "Heavy cleave darkens the screen, +50% damage.", r: "rare" },
        { n: "Corona Aura", e: "Passive burn aura around you (3 tiles).", r: "rare" },
        { n: "Solar Ignition", e: "Burned enemies explode on death.", r: "rare" },
        { n: "Supernova Strike", e: "Finisher triggers a 10-tile supernova.", r: "legendary", c: 5 },
      ]},
      { name: "aegis", nodes: [
        { n: "Parry Frames", e: "First 4 frames of swing grant invulnerability." },
        { n: "Damage Reflection", e: "Parried projectiles reflect at +50% damage.", r: "rare" },
        { n: "Dawn Dash", e: "Dash forward with i-frames (3s cd).", r: "rare" },
        { n: "Bulwark", e: "Standing still 1s grants +20 defense.", r: "rare" },
        { n: "Eternal Guard", e: "Parry window doubled; reflects melee too.", r: "legendary", c: 4 },
      ]},
      { name: "weight", nodes: [
        { n: "Heavy Strikes", e: "+50% knockback, -10% speed." },
        { n: "Ground Slam", e: "Down-swing creates a shockwave.", r: "rare" },
        { n: "Crater", e: "Ground slam leaves a damaging crater (5s).", r: "rare" },
        { n: "Meteor Drop", e: "Air-slam rains meteors around you.", r: "rare" },
        { n: "Gravity Well", e: "Slams pull enemies inward before bursting.", r: "legendary", c: 5 },
      ]},
      { name: "absorb", nodes: [
        { n: "Memory Codex I", e: "Unlock Memory Rune slot 1.", r: "rare" },
        { n: "Memory Codex II", e: "Unlock Memory Rune slot 2.", r: "rare" },
        { n: "Memory Codex III", e: "Unlock Memory Rune slot 3.", r: "rare" },
        { n: "Resonance Tuning", e: "Memorize any sword's signature swing.", r: "legendary", c: 5 },
        { n: "Blademaster's Soul", e: "Equip 5 Runes; fuse two swings into a hybrid.", r: "legendary", c: 8 },
      ]},
    ]),
    loreAbsorption:
      "Solbrand's capstone opens the Blade Memory Codex. Memorize the Terra Blade's beam, the Influx Waver's double-slash, the Horseman's Blade's pumpkin summon — all slotted as Memory Runes you can wield simultaneously.",
  },
  {
    id: "cannon",
    name: "Voidcannon",
    epithet: "the Pulse Driver",
    classLabel: "Ranged (Bullets)",
    fantasy: "A kinetic-energy cannon that merges sci-fi engineering with cosmic magic.",
    image: "/cosmic/weapon-cannon.png",
    accent: "#3dd6c4",
    accentSoft: "rgba(61,214,196,0.15)",
    stats: [
      { label: "Damage", value: "Scaling (Lv × 2.8)" },
      { label: "Fire rate", value: "9 → 3 frames" },
      { label: "Magazine", value: "6 → ∞ (with runes)" },
      { label: "Knockback", value: "Variable" },
    ],
    baseProjectiles: ["Kinetic Slug", "Plasma Bolt", "Void Lance"],
    branches: [
      { id: "slug", name: "Slug Genesis", icon: "◉", color: "teal", description: "New ammo types: plasma, void, emp." },
      { id: "rapid", name: "Rapid Fire", icon: "⚡", color: "teal", description: "Fire rate, mag size, reload speed." },
      { id: "precision", name: "Precision", icon: "⊕", color: "teal", description: "Accuracy, headshots, crit mult." },
      { id: "ordnance", name: "Heavy Ordnance", icon: "☄", color: "teal", description: "Charged shots, artillery mode, overcharge." },
      { id: "recoil", name: "Recoil Engineering", icon: "↯", color: "teal", description: "Recoil dash, rocket-jump, knockback immunity." },
      { id: "absorb", name: "Lore Absorption", icon: "⭐", color: "teal", description: "Absorb every gun's signature bullet.", capstone: true },
    ],
    skillTree: buildTree([
      { name: "slug", nodes: [
        { n: "Plasma Bolt", e: "Alternate ammo: +25% damage, ignites enemies." },
        { n: "Void Lance", e: "Alternate ammo: pierces 5 enemies, +40% damage.", r: "rare" },
        { n: "EMP Slug", e: "Alternate ammo: stuns mechanical foes 1s.", r: "rare" },
        { n: "Cryo Shot", e: "Alternate ammo: slows enemies 50% for 3s.", r: "rare" },
        { n: "Singularity Round", e: "Charged shot spawns a 4s black hole.", r: "legendary", c: 3 },
      ]},
      { name: "rapid", nodes: [
        { n: "Trigger Mod", e: "Fire rate +20%." },
        { n: "Extended Mag", e: "+4 shots before reload.", r: "rare" },
        { n: "Fast Reload", e: "Reload time -40%.", r: "rare" },
        { n: "Dual-Trigger", e: "Every 5th shot is free and deals +100%.", r: "rare" },
        { n: "Hyperfire", e: "No reload; ammo regenerates over time.", r: "legendary", c: 4 },
      ]},
      { name: "precision", nodes: [
        { n: "Stabilizer", e: "Spread -50%." },
        { n: "Headshot", e: "Hits to enemy heads deal +200% damage.", r: "rare" },
        { n: "Crit Multiplier", e: "Crit damage +50% (stacks).", r: "rare" },
        { n: "Scope Zoom", e: "Right-click to zoom; zoomed shots always crit.", r: "rare" },
        { n: "Deadeye", e: "Standing still 1s: next shot is a guaranteed 3x crit.", r: "legendary", c: 3 },
      ]},
      { name: "ordnance", nodes: [
        { n: "Charged Shot", e: "Hold fire to charge; +150% damage at full.", r: "rare" },
        { n: "Artillery Mode", e: "Deploy: stationary, fire rate +200%.", r: "rare" },
        { n: "Beam Overcharge", e: "Full charge becomes a continuous beam (2s).", r: "rare" },
        { n: "Cluster Shell", e: "Shots split into 4 on impact.", r: "rare" },
        { n: "Orbital Strike", e: "Charged shot calls a satellite beam from above.", r: "legendary", c: 5 },
      ]},
      { name: "recoil", nodes: [
        { n: "Recoil Dash", e: "Firing while moving dashes you backward." },
        { n: "Rocket Jump", e: "Down-fire launches you upward (no fall damage).", r: "rare" },
        { n: "Knockback Immunity", e: "Immune to your own recoil & knockback.", r: "rare" },
        { n: "Thruster Boots", e: "Recoil dash doesn't cost ammo.", r: "rare" },
        { n: "Kinetic Glide", e: "Chain recoil dashes mid-air indefinitely.", r: "legendary", c: 4 },
      ]},
      { name: "absorb", nodes: [
        { n: "Memory Codex I", e: "Unlock Memory Rune slot 1.", r: "rare" },
        { n: "Memory Codex II", e: "Unlock Memory Rune slot 2.", r: "rare" },
        { n: "Memory Codex III", e: "Unlock Memory Rune slot 3.", r: "rare" },
        { n: "Resonance Tuning", e: "Memorize any gun's signature bullet type.", r: "legendary", c: 5 },
        { n: "Arsenal of Eternity", e: "Equip 5 Runes; hot-swap ammo types mid-combat.", r: "legendary", c: 8 },
      ]},
    ]),
    loreAbsorption:
      "Voidcannon's capstone opens the Arsenal Memory Codex. Memorize the Megashark's rapid-fire burst, the Sniper Rifle's piercing shot, the Chain Gun's spray — all slotted as Memory Runes you wield simultaneously.",
  },
  {
    id: "book",
    name: "Grimoire",
    epithet: "of the Eternal",
    classLabel: "Magic",
    fantasy: "A spellbook sourcing its incantations from Aethon's infinite memory.",
    image: "/cosmic/weapon-book.png",
    accent: "#b388ff",
    accentSoft: "rgba(179,136,255,0.15)",
    stats: [
      { label: "Damage", value: "Scaling (Lv × 2.6 + mana%)" },
      { label: "Mana cost", value: "8 → 2 (with runes)" },
      { label: "Cast speed", value: "20 → 6 frames" },
      { label: "Knockback", value: "Low → High" },
    ],
    baseProjectiles: ["Arcane Bolt", "Homing Spark", "Star Shard"],
    branches: [
      { id: "mana", name: "Mana Flow", icon: "💧", color: "purple", description: "Max mana, regen, cost reduction." },
      { id: "element", name: "Elemental Genesis", icon: "🔥", color: "purple", description: "Fire, frost, storm, void elements." },
      { id: "proj", name: "Projectile Evolution", icon: "✺", color: "purple", description: "Split, homing, chain, multi-cast." },
      { id: "convert", name: "Arcane Conversion", icon: "☯", color: "purple", description: "Mana↔HP, lifesteal, scaling." },
      { id: "cosmic", name: "Cosmic Spells", icon: "🌌", color: "purple", description: "Black Hole, Supernova, Time Dilation." },
      { id: "absorb", name: "Lore Absorption", icon: "⭐", color: "purple", description: "Absorb every magic weapon's spell.", capstone: true },
    ],
    skillTree: buildTree([
      { name: "mana", nodes: [
        { n: "Mana Pool", e: "+40 max mana." },
        { n: "Flowing Mana", e: "Mana regen +50%.", r: "rare" },
        { n: "Efficient Cast", e: "Mana cost -25%.", r: "rare" },
        { n: "Mana Well", e: "Passive: restore 5 mana/sec while standing still.", r: "rare" },
        { n: "Bottomless Reserve", e: "Casting below 20 mana is free.", r: "legendary", c: 3 },
      ]},
      { name: "element", nodes: [
        { n: "Fire Bolt", e: "Bolts ignite enemies (burn DoT)." },
        { n: "Frost Shard", e: "Bolts slow enemies 40% for 3s.", r: "rare" },
        { n: "Storm Arc", e: "Bolts chain to 2 nearby enemies.", r: "rare" },
        { n: "Void Lance", e: "Bolts pierce 3 enemies, +30% damage.", r: "rare" },
        { n: "Elemental Convergence", e: "Bolts rotate elements every cast; all at once.", r: "legendary", c: 4 },
      ]},
      { name: "proj", nodes: [
        { n: "Split Bolt", e: "Bolts split into 2 on impact." },
        { n: "Homing Spark", e: "Bolts home toward nearest enemy.", r: "rare" },
        { n: "Chain Cast", e: "Bolts bounce between 3 enemies.", r: "rare" },
        { n: "Familiar Orb", e: "Spawns an orbital spell-familiar (3 bolts/sec).", r: "rare" },
        { n: "Multicast", e: "Every cast fires 3 additional bolts for free.", r: "legendary", c: 5 },
      ]},
      { name: "convert", nodes: [
        { n: "Mana Shield", e: "Damage taken drains mana before HP." },
        { n: "Mana→HP", e: "Casting heals 2 HP per 10 mana spent.", r: "rare" },
        { n: "HP→Mana", e: "Lose 5 HP to restore 30 mana (active).", r: "rare" },
        { n: "Desperation", e: "Damage scales with % of missing mana.", r: "rare" },
        { n: "Eternal Cycle", e: "Killing an enemy restores 30% mana & 10% HP.", r: "legendary", c: 4 },
      ]},
      { name: "cosmic", nodes: [
        { n: "Black Hole", e: "Charged cast spawns a 4s gravity well." },
        { n: "Supernova", e: "Charged cast: 10-tile cosmic explosion.", r: "rare" },
        { n: "Time Dilation", e: "Charged cast: slows all enemies 60% for 4s.", r: "rare" },
        { n: "Starfall", e: "Passive: a star falls on a random enemy every 3s.", r: "rare" },
        { n: "Reality Tear", e: "Charged cast rips a portal; bolts exit from a 2nd portal.", r: "legendary", c: 6 },
      ]},
      { name: "absorb", nodes: [
        { n: "Memory Codex I", e: "Unlock Memory Rune slot 1.", r: "rare" },
        { n: "Memory Codex II", e: "Unlock Memory Rune slot 2.", r: "rare" },
        { n: "Memory Codex III", e: "Unlock Memory Rune slot 3.", r: "rare" },
        { n: "Resonance Tuning", e: "Memorize any magic weapon's projectile & effect.", r: "legendary", c: 5 },
        { n: "Omni-Grimoire", e: "Equip 5 Runes; cast all simultaneously in a storm.", r: "legendary", c: 8 },
      ]},
    ]),
    loreAbsorption:
      "The Grimoire's capstone opens the Spell Memory Codex — the fantasy core of the mod. Memorize the Last Prism's converging beam, the Lunar Flare's rain, the Demon Scythe's boomerang, the Magic Dagger's throw. The Grimoire becomes every magic weapon at once, wielded as one.",
  },
];

export function getWeapon(id: WeaponId): Weapon {
  return WEAPONS.find((w) => w.id === id)!;
}

// ---------------------------------------------------------------------------
// Bosses
// ---------------------------------------------------------------------------

export const BOSSES: Boss[] = [
  {
    id: "aethon",
    name: "Aethon, the Primordial Light",
    tier: "final",
    phaseLabel: "5-Phase Final Boss",
    hp: "2,400,000 / phase",
    description:
      "The cosmic entity whose fractured power you have been wielding. When your Genesis Shard reaches full resonance, Aethon stirs — and acknowledges you as a peer consciousness by trying to unmake you.",
    mechanics: [
      "Phase 1 — Stardust: Aethon fires star-bolts in spiral patterns.",
      "Phase 2 — Nebula: AoE clouds blind and burn; arena warps.",
      "Phase 3 — Gravity: Gravity flips every 8s; ground becomes ceiling.",
      "Phase 4 — Black Hole: A singularity pulls you in while spawning adds.",
      "Phase 5 — Acknowledgment: Aethon wields YOUR absorbed abilities back at you.",
    ],
    unlock: "Genesis Shard reaches Lv 150 (The Awakening)",
    accent: "#f5c451",
  },
  {
    id: "echo-blade",
    name: "Echo of the First Wielder",
    tier: "echo",
    phaseLabel: "Echo Boss",
    hp: "180,000",
    description:
      "A shadow-clone of the very first soul to bind a Genesis Shard — a swordsman whose name is lost to time. They appear at Aethon's Stirring to test your worthiness.",
    mechanics: [
      "Mimics a fully-built Solbrand skill tree.",
      "Parries your attacks with i-frames.",
      "Phase 2 at 40% HP: unleashes Reality Cleave relentlessly.",
    ],
    unlock: "Genesis Shard reaches Lv 100",
    accent: "#ff9a3c",
  },
  {
    id: "echo-archer",
    name: "Echo of the Star-Archer",
    tier: "echo",
    phaseLabel: "Echo Boss",
    hp: "160,000",
    description:
      "The ghost of an archer who once sought to shoot down Aethon itself. Their aim never misses; their arrows curve through dimensions.",
    mechanics: [
      "Fires homing phantom arrows that phase terrain.",
      "Lays Bouncing Betty light-mines across the arena.",
      "Phase 2: calls a Starfall Storm every 6s.",
    ],
    unlock: "Genesis Shard reaches Lv 110",
    accent: "#f5c451",
  },
  {
    id: "witness",
    name: "The Witness",
    tier: "cosmic",
    phaseLabel: "Cosmic NPC / Optional Boss",
    hp: "—",
    description:
      "A wandering cosmic entity that observes your progress. It narrates lore as you level, sells Memory Runes, and — if attacked — reveals its true power.",
    mechanics: [
      "Normally non-hostile; sells Resonance Shards & runes.",
      "If attacked: 3-phase optional superboss.",
      "Drops the 'Eye of the Witness' cosmetic on defeat.",
    ],
    unlock: "Genesis Shard reaches Lv 50 (The Hollowing)",
    accent: "#b388ff",
  },
  {
    id: "rift-keeper",
    name: "The Rift-Keeper",
    tier: "cosmic",
    phaseLabel: "Cosmic Mini-Boss",
    hp: "95,000",
    description:
      "A guardian that emerges from Dimensional Rifts. It exists half in Terraria, half in the void between worlds.",
    mechanics: [
      "Teleports through rifts across the arena.",
      "Fires void-lances that pierce terrain.",
      "At 30% HP: seals the rifts, trapping you for a final stand.",
    ],
    unlock: "Genesis Shard reaches Lv 75 (Dimensional Rifts)",
    accent: "#3dd6c4",
  },
  {
    id: "hollow-titan",
    name: "The Hollow Titan",
    tier: "mini",
    phaseLabel: "Mini-Boss",
    hp: "42,000",
    description:
      "A colossal crystalline guardian of the Hollow Sanctum. The first cosmic threat most players face.",
    mechanics: [
      "Slow but devastating slam attacks.",
      "Spawns crystal shards that home in.",
      "Enrages at 50% HP, doubling attack speed.",
    ],
    unlock: "Discover the Hollow Sanctum biome (post 200 max HP)",
    accent: "#7ee3c4",
  },
];

// ---------------------------------------------------------------------------
// Lore timeline
// ---------------------------------------------------------------------------

export const LORE: LoreEntry[] = [
  {
    id: "before",
    title: "Before the World",
    era: "Time Immemorial",
    body: "Aethon existed before Terraria's universe coalesced. Its body was a galaxy; its thoughts were gravitational tides. To seed creation, it shattered itself — every star, every soul, is a splinter of Aethon. A fragment of its true consciousness fell asleep inside the world, buried in stone.",
  },
  {
    id: "worship",
    title: "The Age of Worship",
    era: "Ancient Era",
    body: "Civilizations rose above the sleeping shard, building temples and altars in reverence. They carved runes into stone and offered their dead to the light. In time the civilizations crumbled to dust; only ruined altars remain — one of which now waits for you.",
  },
  {
    id: "shard",
    title: "The Genesis Shard",
    era: "Your Story Begins",
    body: "Deep in the newly-formed Hollow Sanctum, you find a floating mote of pure light beside a runed altar. It pulses in time with your heartbeat. This is the Genesis Shard — a fragment of Aethon's power. It will bond to your soul, and you must choose its form: Bow, Sword, Cannon, or Book.",
  },
  {
    id: "growth",
    title: "The Growth",
    era: "Pre-Hardmode → Hardmode",
    body: "Every enemy you slay feeds the shard a sliver of essence. It levels — infinitely. At Lv 10 it solidifies into a runed artifact; at Lv 50 it stirs cosmic events; at Lv 100 it shines like a captured star. The Witness watches, narrating your ascent.",
  },
  {
    id: "absorption",
    title: "The Memory Codex",
    era: "Late Hardmode",
    body: "As the shard matures, it unlocks the Memory Codex — the ability to absorb the signature abilities of every weapon of its class in the game. A Grimoire can cast the Last Prism's beam AND the Lunar Flare's rain. A Solbrand can swing like the Terra Blade AND the Influx Waver. Your single weapon becomes every weapon at once.",
  },
  {
    id: "awakening",
    title: "The Awakening",
    era: "Endgame",
    body: "At Lv 150, the shard's resonance reaches Aethon's slumbering consciousness. It stirs. The sky tears open. The final confrontation begins — not as a war, but as an acknowledgment. Aethon fights you with your own absorbed abilities, to prove you are worthy of being called its peer.",
  },
];
