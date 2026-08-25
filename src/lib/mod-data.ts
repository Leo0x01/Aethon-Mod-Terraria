// Central data module for the "Aethon, la Luz Primordial" Terraria mod showcase.
//
// The mod's combat system has THREE primary branches (Distancia / Cuerpo a
// Cuerpo / Artes Mágicas). The Genesis Shard fragment adapts its shape to the
// branch the player develops first.

export type BranchId = "distance" | "melee" | "magic";

export type NodeRarity = "common" | "rare" | "legendary";

export interface SkillNode {
  id: string;
  name: string;
  branch: string; // sub-branch id within the branch's tree
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

export interface Branch {
  id: BranchId;
  name: string; // weapon name (e.g. "Lumina, la Arcoestelar")
  epithet: string;
  classLabel: string; // e.g. "Distancia"
  fantasy: string;
  image: string;
  accent: string; // hex accent for glows
  accentSoft: string;
  stats: { label: string; value: string }[];
  baseProjectiles: string[];
  branches: SkillBranch[]; // sub-ramas of the skill tree
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
  shardDrop: number; // Resonance Shards dropped on first defeat
  repeatDrop: number; // Shards dropped on repeat kills
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
  { level: 10, label: "Primer despertar", desc: "El fragmento se solidifica — gana un glifo rúnico." },
  { level: 25, label: "Lluvia de luz estelar", desc: "Evento cósmico: lluvia de meteoros ambiental, mineral raro." },
  { level: 50, label: "El Sagrario Hueco se extiende", desc: "El bioma se propaga, nuevos mobs emergen." },
  { level: 75, label: "Rifts dimensionales", desc: "Mini-mazmorras aparecen con loot único." },
  { level: 100, label: "El agitar de Aethon", desc: "Ecos de portadores anteriores aparecen como jefes." },
  { level: 150, label: "El despertar", desc: "Jefe final Aethon se vuelve disponible." },
  { level: 200, label: "Forma Ascendida", desc: "El fragmento toma su verdadera forma galáctica." },
];

// ---------------------------------------------------------------------------
// Branches (3 primary combat branches)
// ---------------------------------------------------------------------------

// Helper to build constellation nodes on a radial layout per sub-branch.
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

export const BRANCHES: Branch[] = [
  // -----------------------------------------------------------------
  // DISTANCIA — Lumina, la Arcoestelar
  // -----------------------------------------------------------------
  {
    id: "distance",
    name: "Lumina, la Arcoestelar",
    epithet: "Forma adaptable de distancia",
    classLabel: "Distancia",
    fantasy:
      "Un arma de distancia adaptable: arco estelar,arma de munición o arrojadiza. Dispara flechas de luz que buscan su marca a través del vacío.",
    image: "/cosmic/weapon-bow.png",
    accent: "#f5c451",
    accentSoft: "rgba(245,196,81,0.15)",
    stats: [
      { label: "Daño", value: "Escalado (Lv × 2.4)" },
      { label: "Tensado", value: "12 → 4 frames" },
      { label: "Munición", value: "Flechas de luz (gratis)" },
      { label: "Knockback", value: "Moderado" },
    ],
    baseProjectiles: ["Flecha estelar", "Haz perforante", "Mote buscador"],
    branches: [
      { id: "projectiles", name: "Génesis de Proyectiles", icon: "➶", color: "amber", description: "Nuevos tipos de munición/proyectil." },
      { id: "quiver", name: "Maestría de Carcaj", icon: "🎒", color: "amber", description: "Más flechas por disparo, tensado más rápido." },
      { id: "hunter", name: "Marca del Cazador", icon: "◎", color: "amber", description: "Etiqueta, rastrea y acumula crítico." },
      { id: "celestial", name: "Disparos Celestiales", icon: "✦", color: "amber", description: "Salva de meteoros, destello solar, eclipse." },
      { id: "phantom", name: "Carcaj Fantasma", icon: "◈", color: "amber", description: "Flechas etéreas que atraviesan el terreno." },
      { id: "absorb", name: "Absorción de Lore", icon: "⭐", color: "amber", description: "Absorbe el comportamiento de cada arma de distancia.", capstone: true },
    ],
    skillTree: buildTree([
      { name: "projectiles", nodes: [
        { n: "Flecha estelar", e: "Proyectil base de luz; ignora 5 de defensa." },
        { n: "Virote de vacío", e: "Munición alternativa: +30% daño, drena 4 maná.", r: "rare" },
        { n: "Disparo dividido", e: "Dispara 2 proyectiles en abanico.", r: "rare" },
        { n: "Estrella guiada", e: "Los proyectiles curvan hacia enemigos en 18 tiles.", r: "rare" },
        { n: "Salva triple", e: "Cada 3er disparo lanza 3 virotes buscadores.", r: "legendary", c: 3 },
      ]},
      { name: "quiver", nodes: [
        { n: "Tiro rápido", e: "Tiempo de tensado -15%." },
        { n: "Cuerda doble", e: "+1 proyectil por disparo.", r: "rare" },
        { n: "Carcaj infinito", e: "No consume munición base.", r: "rare" },
        { n: "Ráfaga", e: "Mantén para disparar 6 proyectiles en secuencia.", r: "rare" },
        { n: "Tormenta de estrellas", e: "Disparo cargado llueve 12 proyectiles del cielo.", r: "legendary", c: 4 },
      ]},
      { name: "hunter", nodes: [
        { n: "Etiqueta", e: "Los golpes marcan enemigos (+10% daño recibido)." },
        { n: "Acumulación de crítico", e: "+4% crítico por golpe en marcados (máx 40%).", r: "rare" },
        { n: "Punto débil", e: "Enemigos marcados muestran un punto débil (siempre crítico).", r: "rare" },
        { n: "Foco del cazador", e: "Estar quieto 1s duplica el crítico.", r: "rare" },
        { n: "Marca letal", e: "Matar a un enemigo marcado resetea todos los cooldowns.", r: "legendary", c: 3 },
      ]},
      { name: "celestial", nodes: [
        { n: "Salva de meteoros", e: "Alt-fuego: 3 meteoros caen en el cursor.", r: "rare" },
        { n: "Destello solar", e: "Los proyectiles incendian (DoT quemadura).", r: "rare" },
        { n: "Eclipse", e: "Disparo cargado ciega + quema a toda la pantalla (10s cd).", r: "rare" },
        { n: "Cascada estelar", e: "Cada golpe genera 2 mini-estrellas.", r: "rare" },
        { n: "Supernova", e: "Proyectil cargado explota en supernova de 12 tiles.", r: "legendary", c: 5 },
      ]},
      { name: "phantom", nodes: [
        { n: "Disparo fase", e: "Los proyectiles atraviesan 3 tiles de terreno.", r: "rare" },
        { n: "Ricochet", e: "Rebota en paredes hasta 2 veces.", r: "rare" },
        { n: "Betty rebotadora", e: "Los fallos generan minas de luz estacionarias.", r: "rare" },
        { n: "Cadena fantasma", e: "Rebota entre 3 enemigos.", r: "rare" },
        { n: "Forma etérea", e: "Todos los proyectiles atraviesan terreno Y perforan 5 enemigos.", r: "legendary", c: 4 },
      ]},
      { name: "absorb", nodes: [
        { n: "Códex de memoria I", e: "Desbloquea slot 1 de Runa de Memoria.", r: "rare" },
        { n: "Códex de memoria II", e: "Desbloquea slot 2 de Runa de Memoria.", r: "rare" },
        { n: "Códex de memoria III", e: "Desbloquea slot 3 de Runa de Memoria.", r: "rare" },
        { n: "Afinación de resonancia", e: "Memoriza el comportamiento de cualquier arma de distancia del juego base + mods.", r: "legendary", c: 5 },
        { n: "Carcaj omnisciente", e: "Equipar 5 Runas simultáneamente; fusiona dos en una.", r: "legendary", c: 8 },
      ]},
    ]),
    loreAbsorption:
      "La sub-rama capstone de Lumina abre el Códex de Memoria: un registro de cada arma de distancia de Terraria (y mods cargados). Invierte Fragmentos de Resonancia para memorizar el comportamiento de un arma — luego la equipas como Runa de Memoria junto a las demás. Lumina se vuelve cada arma de distancia a la vez.",
  },

  // -----------------------------------------------------------------
  // CUERPO A CUERPO — Solbrand, Filo del Alba
  // -----------------------------------------------------------------
  {
    id: "melee",
    name: "Solbrand, Filo del Alba",
    epithet: "Hoja de luz condensada",
    classLabel: "Cuerpo a Cuerpo",
    fantasy:
      "Una hoja de luz condensada que corta la trama misma de la realidad. Espada, lanza o yoyo: la forma es adaptable, el filo siempre es el alba.",
    image: "/cosmic/weapon-sword.png",
    accent: "#ff9a3c",
    accentSoft: "rgba(255,154,60,0.15)",
    stats: [
      { label: "Daño", value: "Escalado (Lv × 3.1)" },
      { label: "Tiempo de uso", value: "18 → 9 frames" },
      { label: "Alcance", value: "Cuerpo a cuerpo + haz (hasta 14 tiles)" },
      { label: "Knockback", value: "Fuerte" },
    ],
    baseProjectiles: ["Corte del alba", "Rayo solar", "Pulso de corona"],
    branches: [
      { id: "blade", name: "Génesis de Hoja", icon: "⚔", color: "orange", description: "Nuevos tipos de corte: onda, rayo, remolino." },
      { id: "combo", name: "Maestría de Combo", icon: "🔥", color: "orange", description: "Medidor de combo escalable y remates." },
      { id: "solar", name: "Ira Solar", icon: "☀", color: "orange", description: "Destello solar, corte de eclipse, aura de corona." },
      { id: "aegis", name: "Égida del Alba", icon: "🛡", color: "orange", description: "Frames de parry, reflexión, dashes invulnerables." },
      { id: "weight", name: "Peso de Estrellas", icon: "★", color: "orange", description: "Golpes pesados, ondas de choque, knockback." },
      { id: "absorb", name: "Absorción de Lore", icon: "⭐", color: "orange", description: "Absorbe el balanceo de cada espada/lanza/yoyo.", capstone: true },
    ],
    skillTree: buildTree([
      { name: "blade", nodes: [
        { n: "Onda de corte", e: "Cada golpe emite onda frontal (6 tiles)." },
        { n: "Corte de rayo", e: "Golpe cargado dispara rayo de 12 tiles.", r: "rare" },
        { n: "Remolino", e: "Alt-fuego: giro que golpea alrededor.", r: "rare" },
        { n: "Combo de estocada", e: "Combo de 3 golpes termina en estocada perforante.", r: "rare" },
        { n: "Corte de realidad", e: "Corte cargado corta terreno Y enemigos.", r: "legendary", c: 3 },
      ]},
      { name: "combo", nodes: [
        { n: "Medidor de combo", e: "Golpes consecutivos acumulan combo (máx 10)." },
        { n: "Remate", e: "A combo 10, +200% daño en el siguiente golpe.", r: "rare" },
        { n: "Impulso", e: "El combo no decae al moverse.", r: "rare" },
        { n: "Parry-Riposte", e: "Bloquear + contraataque resetea combo y suma 5.", r: "rare" },
        { n: "Filo infinito", e: "Sin tope de combo; +5% daño por golpe más allá de 10.", r: "legendary", c: 4 },
      ]},
      { name: "solar", nodes: [
        { n: "Corte de destello solar", e: "Los golpes incendian (ciego + DoT).", r: "rare" },
        { n: "Corte de eclipse", e: "Corte pesado oscurece la pantalla, +50% daño.", r: "rare" },
        { n: "Aura de corona", e: "Aura pasiva que quema (3 tiles).", r: "rare" },
        { n: "Ignición solar", e: "Enemigos quemados explotan al morir.", r: "rare" },
        { n: "Golpe de supernova", e: "Remate detona supernova de 10 tiles.", r: "legendary", c: 5 },
      ]},
      { name: "aegis", nodes: [
        { n: "Frames de parry", e: "Los primeros 4 frames del golpe dan invulnerabilidad." },
        { n: "Reflexión de daño", e: "Los proyectiles parry rebotan con +50% daño.", r: "rare" },
        { n: "Dash del alba", e: "Dash con i-frames (3s cd).", r: "rare" },
        { n: "Baluarte", e: "Estar quieto 1s otorga +20 defensa.", r: "rare" },
        { n: "Guardia eterna", e: "Ventana de parry duplicada; refleja también cuerpo a cuerpo.", r: "legendary", c: 4 },
      ]},
      { name: "weight", nodes: [
        { n: "Golpes pesados", e: "+50% knockback, -10% velocidad." },
        { n: "Golpe al suelo", e: "Golpe hacia abajo genera onda de choque.", r: "rare" },
        { n: "Cráter", e: "El golpe al suelo deja un cráter dañino (5s).", r: "rare" },
        { n: "Caída de meteorito", e: "Golpe aéreo llueve meteoritos alrededor.", r: "rare" },
        { n: "Pozo de gravedad", e: "Los golpes al suelo atraen enemigos antes de estallar.", r: "legendary", c: 5 },
      ]},
      { name: "absorb", nodes: [
        { n: "Códex de memoria I", e: "Desbloquea slot 1 de Runa de Memoria.", r: "rare" },
        { n: "Códex de memoria II", e: "Desbloquea slot 2 de Runa de Memoria.", r: "rare" },
        { n: "Códex de memoria III", e: "Desbloquea slot 3 de Runa de Memoria.", r: "rare" },
        { n: "Afinación de resonancia", e: "Memoriza cualquier espada, lanza o yoyo del juego base + mods.", r: "legendary", c: 5 },
        { n: "Alma del maestro de hojas", e: "Equipar 5 Runas; fusiona dos golpes en uno híbrido.", r: "legendary", c: 8 },
      ]},
    ]),
    loreAbsorption:
      "La sub-rama capstone de Solbrand abre el Códex de Memoria de Hojas. Memoriza el rayo verde del Terra Blade, el doble corte del Influx Waver, las calabazas del Horseman's Blade — todas equipadas simultáneamente como Runas de Memoria.",
  },

  // -----------------------------------------------------------------
  // ARTES MÁGICAS — Grimorio del Eterno (Magic + Summoner fusionados)
  // -----------------------------------------------------------------
  {
    id: "magic",
    name: "Grimorio del Eterno",
    epithet: "Grimorio flotante (magia + invocación)",
    classLabel: "Artes Mágicas",
    fantasy:
      "Un grimorio flotante cuyas incantaciones fluyen de la memoria infinita de Aethon. Lanza hechizos cósmicos Y mantiene minions que heredan el poder del fragmento.",
    image: "/cosmic/weapon-book.png",
    accent: "#b388ff",
    accentSoft: "rgba(179,136,255,0.15)",
    stats: [
      { label: "Daño", value: "Escalado (Lv × 2.6 + % maná)" },
      { label: "Coste de maná", value: "8 → 2 (con runas)" },
      { label: "Velocidad de lanzado", value: "20 → 6 frames" },
      { label: "Knockback", value: "Bajo → Alto" },
    ],
    baseProjectiles: ["Bolt arcano", "Chispa buscadora", "Esquirla estelar"],
    branches: [
      { id: "mana", name: "Flujo de Maná", icon: "💧", color: "purple", description: "Maná máximo, regeneración, reducción de coste." },
      { id: "element", name: "Génesis Elemental", icon: "🔥", color: "purple", description: "Fuego, escarcha, tormenta, vacío." },
      { id: "proj", name: "Evolución de Proyectiles", icon: "✺", color: "purple", description: "División, homing, cadena, multilanzamiento." },
      { id: "convert", name: "Conversión Arcana", icon: "☯", color: "purple", description: "Maná↔HP, robo de vida, escalado." },
      { id: "cosmic", name: "Hechizos Cósmicos", icon: "🌌", color: "purple", description: "Agujero negro, supernova, dilatación temporal." },
      { id: "summon", name: "Maestría de Invocación", icon: "👹", color: "purple", description: "Minions del fragmento, slots, empoderamiento." },
      { id: "absorb", name: "Absorción de Lore", icon: "⭐", color: "purple", description: "Absorbe cada arma mágica O de invocador.", capstone: true },
    ],
    skillTree: buildTree([
      { name: "mana", nodes: [
        { n: "Reserva de maná", e: "+40 maná máximo." },
        { n: "Maná fluyente", e: "Regeneración de maná +50%.", r: "rare" },
        { n: "Lanzamiento eficiente", e: "-25% coste de maná.", r: "rare" },
        { n: "Pozo de maná", e: "Pasivo: restaura 5 maná/seg quieto.", r: "rare" },
        { n: "Reserva inagotable", e: "Lanzar con <20 maná es gratis.", r: "legendary", c: 3 },
      ]},
      { name: "element", nodes: [
        { n: "Bolt de fuego", e: "Los bolts incendian (DoT)." },
        { n: "Esquirla de escarcha", e: "Los bolts ralentizan 40% por 3s.", r: "rare" },
        { n: "Arco de tormenta", e: "Los bolts saltan a 2 enemigos cercanos.", r: "rare" },
        { n: "Virote de vacío", e: "Los bolts perforan 3, +30% daño.", r: "rare" },
        { n: "Convergencia elemental", e: "Los bolts rotan elementos cada lanzamiento; todos a la vez.", r: "legendary", c: 4 },
      ]},
      { name: "proj", nodes: [
        { n: "Bolt dividido", e: "Los bolts se dividen en 2 al impactar." },
        { n: "Chispa guiada", e: "Los bolts homing al enemigo más cercano.", r: "rare" },
        { n: "Cadena de lanzamiento", e: "Los bolts saltan entre 3 enemigos.", r: "rare" },
        { n: "Orbe familiar", e: "Genera un familiar orbital (3 bolts/seg).", r: "rare" },
        { n: "Multilanzamiento", e: "Cada lanzamiento dispara 3 bolts extra gratis.", r: "legendary", c: 5 },
      ]},
      { name: "convert", nodes: [
        { n: "Escudo de maná", e: "El daño drena maná antes que HP." },
        { n: "Maná → HP", e: "Lanzar cura 2 HP por 10 maná gastado.", r: "rare" },
        { n: "HP → Maná", e: "Pierde 5 HP para restaurar 30 maná.", r: "rare" },
        { n: "Desesperación", e: "El daño escala con % de maná faltante.", r: "rare" },
        { n: "Ciclo eterno", e: "Matar restaura 30% maná + 10% HP.", r: "legendary", c: 4 },
      ]},
      { name: "cosmic", nodes: [
        { n: "Agujero negro", e: "Lanzamiento cargado genera pozo gravitacional de 4s." },
        { n: "Supernova", e: "Lanzamiento cargado: explosión cósmica de 10 tiles.", r: "rare" },
        { n: "Dilatación temporal", e: "Lanzamiento cargado: ralentiza enemigos 60% por 4s.", r: "rare" },
        { n: "Lluvia de estrellas", e: "Pasivo: una estrella cae cada 3s.", r: "rare" },
        { n: "Desgarro de realidad", e: "Cargado abre portal; los bolts salen de un 2º portal.", r: "legendary", c: 6 },
      ]},
      { name: "summon", nodes: [
        { n: "Minion base", e: "Invoca 1 minion del fragmento (estrella orbitante que dispara)." },
        { n: "Minion +1", e: "+1 slot de minion.", r: "rare" },
        { n: "Minion empoderado", e: "Los minions heredan +10% del daño del grimorio.", r: "rare" },
        { n: "Minion torre", e: "Los minions pueden estacionarse (modo defensivo).", r: "rare" },
        { n: "Enjambre estelar", e: "+3 slots; los minions disparan bolts al atacar.", r: "legendary", c: 5 },
      ]},
      { name: "absorb", nodes: [
        { n: "Códex de memoria I", e: "Desbloquea slot 1 de Runa de Memoria.", r: "rare" },
        { n: "Códex de memoria II", e: "Desbloquea slot 2 de Runa de Memoria.", r: "rare" },
        { n: "Códex de memoria III", e: "Desbloquea slot 3 de Runa de Memoria.", r: "rare" },
        { n: "Afinación de resonancia", e: "Memoriza cualquier arma mágica O de invocador del juego base + mods.", r: "legendary", c: 5 },
        { n: "Grimorio omnisciente", e: "Equipar 5 Runas; lanzar todas simultáneamente en una tormenta.", r: "legendary", c: 8 },
      ]},
    ]),
    loreAbsorption:
      "La sub-rama capstone del Grimorio abre el Códex de Memoria de Hechizos — el corazón fantástico del mod. Memoriza el rayo convergente del Last Prism, la lluvia del Lunar Flare, el bumerán del Demon Scythe, los minions del Stardust Dragon. El grimorio se vuelve cada arma mágica y de invocador a la vez.",
  },
];

export function getBranch(id: BranchId): Branch {
  return BRANCHES.find((b) => b.id === id)!;
}

// ---------------------------------------------------------------------------
// Bosses
// ---------------------------------------------------------------------------

export const BOSSES: Boss[] = [
  {
    id: "aethon",
    name: "Aethon, la Luz Primordial",
    tier: "final",
    phaseLabel: "Jefe final de 5 fases",
    hp: "2,400,000 / fase",
    description:
      "La entidad cósmica cuyo poder fracturado has estado empuñando. Cuando tu Fragmento Génesis alcanza resonancia completa, Aethon despierta — y te reconoce como una consciencia par al intentar deshacerte.",
    mechanics: [
      "Fase 1 — Polvo estelar: Aethon dispara pernos en espiral.",
      "Fase 2 — Nebulosa: Nubes AoE ciegan y queman; la arena se deforma.",
      "Fase 3 — Gravedad: La gravedad se invierte cada 8s; el suelo se vuelve techo.",
      "Fase 4 — Agujero negro: Una singularidad te atrae mientras genera adds.",
      "Fase 5 — Reconocimiento: Aethon empuña TUS habilidades absorbidas contra ti.",
    ],
    unlock: "El Fragmento Génesis alcanza Lv 150 (El despertar)",
    accent: "#f5c451",
    shardDrop: 250,
    repeatDrop: 40,
  },
  {
    id: "echo-blade",
    name: "Eco del Primer Portador",
    tier: "echo",
    phaseLabel: "Jefe Eco",
    hp: "180,000",
    description:
      "Una sombra-clon del alma que primero vinculó un Fragmento Génesis — un espadachín cuyo nombre se perdió en el tiempo. Aparece en el Agitar de Aethon para poner a prueba tu valía.",
    mechanics: [
      "Mimicea un árbol de Solbrand completamente construido.",
      "Parry tus ataques con i-frames.",
      "Fase 2 al 40% HP: desata Corte de realidad sin descanso.",
    ],
    unlock: "El Fragmento Génesis alcanza Lv 100",
    accent: "#ff9a3c",
    shardDrop: 120,
    repeatDrop: 18,
  },
  {
    id: "echo-archer",
    name: "Eco de la Arquera Estelar",
    tier: "echo",
    phaseLabel: "Jefe Eco",
    hp: "160,000",
    description:
      "El fantasma de una arquera que una vez buscó derribar a Aethon. Su puntería nunca falla; sus flechas curvan a través de dimensiones.",
    mechanics: [
      "Dispara flechas fantasma homing que atraviesan terreno.",
      "Esparce minas de luz Betty rebotadoras por la arena.",
      "Fase 2: invoca una Tormenta de estrellas cada 6s.",
    ],
    unlock: "El Fragmento Génesis alcanza Lv 110",
    accent: "#f5c451",
    shardDrop: 110,
    repeatDrop: 16,
  },
  {
    id: "witness",
    name: "El Testigo",
    tier: "cosmic",
    phaseLabel: "NPC Cósmico / Jefe opcional",
    hp: "—",
    description:
      "Una entidad cósmica errante que observa tu progreso. Narra lore mientras subes de nivel, vende Runas de Memoria y — si lo atacas — revela su verdadero poder.",
    mechanics: [
      "Normalmente no hostil; vende Fragmentos de Resonancia y runas.",
      "Si lo atacas: superboss opcional de 3 fases.",
      "Suelta el cosmético 'Ojo del Testigo' al derrotarlo.",
    ],
    unlock: "El Fragmento Génesis alcanza Lv 50 (El Sagrario Hueco se extiende)",
    accent: "#b388ff",
    shardDrop: 0,
    repeatDrop: 0,
  },
  {
    id: "rift-keeper",
    name: "El Guardián del Rift",
    tier: "cosmic",
    phaseLabel: "Mini-Jefe Cósmico",
    hp: "95,000",
    description:
      "Un guardián que emerge de los Rifts dimensionales. Existe mitad en Terraria, mitad en el vacío entre mundos.",
    mechanics: [
      "Se teletransporta a través de rifts por la arena.",
      "Dispara virotes de vacío que perforan terreno.",
      "Al 30% HP: sella los rifts, atrapándote para un duelo final.",
    ],
    unlock: "El Fragmento Génesis alcanza Lv 75 (Rifts dimensionales)",
    accent: "#3dd6c4",
    shardDrop: 45,
    repeatDrop: 8,
  },
  {
    id: "hollow-titan",
    name: "El Titán Hueco",
    tier: "mini",
    phaseLabel: "Mini-Jefe",
    hp: "42,000",
    description:
      "Un guardián cristalino colosal del Sagrario Hueco. La primera amenaza cósmica que la mayoría de jugadores enfrenta.",
    mechanics: [
      "Ataques de slam lentos pero devastadores.",
      "Genera esquirlas de cristal que homing.",
      "Enrage al 50% HP, duplicando velocidad de ataque.",
    ],
    unlock: "Descubrir el bioma Sagrario Hueco (post 200 HP máx)",
    accent: "#7ee3c4",
    shardDrop: 8,
    repeatDrop: 2,
  },
];

// ---------------------------------------------------------------------------
// Lore timeline
// ---------------------------------------------------------------------------

export const LORE: LoreEntry[] = [
  {
    id: "before",
    title: "Antes del Mundo",
    era: "Tiempo Inmemorial",
    body: "Aethon existió antes de que el universo de Terraria se coagulara. Su cuerpo era una galaxia; sus pensamientos eran mareas gravitacionales. Para sembrar la creación, se fracturó a sí misma — cada estrella, cada alma, es una astilla de Aethon. Un fragmento de su verdadera consciencia quedó dormido dentro del mundo, enterrado en piedra.",
  },
  {
    id: "worship",
    title: "La Era de la Adoración",
    era: "Era Antigua",
    body: "Civilizaciones florecieron sobre el fragmento dormido, construyendo templos y altares en reverencia. Tallaron runas en piedra y ofrecieron a sus muertos a la luz. Con el tiempo, las civilizaciones se desmoronaron en polvo; solo quedan altares en ruinas — uno de los cuales te espera ahora.",
  },
  {
    id: "shard",
    title: "El Fragmento Génesis",
    era: "Tu Historia Comienza",
    body: "En lo profundo del recién formado Sagrario Hueco, encuentras un mote flotante de luz pura junto a un altar rúnico. Pulsa al ritmo de tu corazón. Este es el Fragmento Génesis — un fragmento del poder de Aethon. Se vinculará a tu alma, y su forma se adaptará a la rama de combate que desarrolles primero: Distancia, Cuerpo a Cuerpo o Artes Mágicas.",
  },
  {
    id: "growth",
    title: "El Crecimiento",
    era: "Pre-Hardmode → Hardmode",
    body: "Cada enemigo que matas alimenta el fragmento con una esquirla de esencia. Sube de nivel — infinitamente. A Lv 10 se solidifica en un artefacto rúnico; a Lv 50 desata eventos cósmicos; a Lv 100 brilla como una estrella capturada. El Testigo observa, narrando tu ascenso.",
  },
  {
    id: "absorption",
    title: "El Códex de Memoria",
    era: "Hardmode tardío",
    body: "A medida que el fragmento madura, desbloquea el Códex de Memoria — la capacidad de absorber las habilidades características de cada arma de su rama en el juego. Un Grimorio puede lanzar el rayo del Last Prism Y la lluvia del Lunar Flare. Un Solbrand puede balancearse como el Terra Blade Y el Influx Waver. Tu arma se vuelve cada arma a la vez.",
  },
  {
    id: "awakening",
    title: "El Despertar",
    era: "Endgame",
    body: "A Lv 150, la resonancia del fragmento alcanza la consciencia dormida de Aethon. Despierta. El cielo se rasga. La confrontación final comienza — no como una guerra, sino como un reconocimiento. Aethon te combate con tus propias habilidades absorbidas, para probar que eres digno de ser llamado su par.",
  },
];

// ---------------------------------------------------------------------------
// Memory Codex — base-game weapons the Genesis Shard can absorb (by branch)
// ---------------------------------------------------------------------------

export interface CodexWeapon {
  id: string;
  name: string;
  branch: BranchId;
  tier: "pre" | "hardmode" | "endgame";
  signature: string; // the signature behavior memorized as a Memory Rune
  effect: string; // what it does once equipped as a rune
  cost: number; // Resonance Shards needed to memorize
  source: string; // where in the game it comes from
}

export const CODEX: CodexWeapon[] = [
  // ---- Artes Mágicas (magia) ----
  { id: "magic-dagger", name: "Magic Dagger", branch: "magic", tier: "pre", signature: "Arco lanzable", effect: "El lanzamiento arroja una daga giratoria que regresa.", cost: 5, source: "Goblin Tinkerer (tras Goblin Army)" },
  { id: "demon-scythe", name: "Demon Scythe", branch: "magic", tier: "pre", signature: "Espiral boomerang", effect: "Lanzamiento dispara una guadaña giratoria que perfora y regresa.", cost: 8, source: "Demonios del inframundo / voodoo demons" },
  { id: "aqua-scepter", name: "Aqua Scepter", branch: "magic", tier: "pre", signature: "Chorro de agua", effect: "Lanzamiento rocía un chorro continuo de agua dañina.", cost: 8, source: "Cofres de santuario de la jungla" },
  { id: "flower-of-fire", name: "Flower of Fire", branch: "magic", tier: "pre", signature: "Bola de fuego", effect: "Lanzamiento lanza una bola de fuego rebotante que explota.", cost: 10, source: "Cofres de sombra del inframundo" },
  { id: "space-gun", name: "Space Gun", branch: "magic", tier: "pre", signature: "Rayo láser", effect: "Lanzamiento dispara un láser de energía rápido y perforante (maná bajo).", cost: 10, source: "Crafteo con meteorito" },
  { id: "magic-missile", name: "Magic Missile", branch: "magic", tier: "pre", signature: "Orbe guiado", effect: "Lanzamiento libera un orbe guiado por tu cursor.", cost: 12, source: "Mazmorra (Skeletron)" },
  { id: "book-of-skulls", name: "Book of Skulls", branch: "magic", tier: "hardmode", signature: "Rociado de calaveras", effect: "Lanzamiento rocía calaveras rebotantes que incendian.", cost: 18, source: "Drop de Skeletron (hardmode)" },
  { id: "crimson-rod", name: "Crimson Rod", branch: "magic", tier: "hardmode", signature: "Nube de sangre", effect: "Lanzamiento invoca una nube de sangre llovedora sobre ti.", cost: 18, source: "Corazones crimson" },
  { id: "sky-fracture", name: "Sky Fracture", branch: "magic", tier: "hardmode", signature: "Esquirla triple", effect: "Lanzamiento dispara 3 esquirlas de cielo homing en abanico.", cost: 22, source: "Crafteo Hallowed (hardmode)" },
  { id: "magnet-sphere", name: "Magnet Sphere", branch: "magic", tier: "hardmode", signature: "Descarga orbital", effect: "Lanzamiento despliega una esfera que dispara láseres a enemigos cercanos.", cost: 24, source: "Mobs de mazmorra hardmode" },
  { id: "leaf-blaster", name: "Leaf Blower", branch: "magic", tier: "hardmode", signature: "Tormenta de hojas", effect: "Lanzamiento dispara un flujo rápido de hojas homing.", cost: 24, source: "Drop de Plantera" },
  { id: "demon-horn", name: "Razorblade Typhoon", branch: "magic", tier: "endgame", signature: "Anillo tifón", effect: "Lanzamiento dispara anillos de agua homing que rebotan 4 veces.", cost: 35, source: "Drop de Duke Fishron" },
  { id: "lunar-flare", name: "Lunar Flare", branch: "magic", tier: "endgame", signature: "Lluvia lunar", effect: "Lanzamiento invoca llamaradas lunares que llueven sobre el cursor.", cost: 40, source: "Fragmentos lunares (Nebula)" },
  { id: "last-prism", name: "Last Prism", branch: "magic", tier: "endgame", signature: "Rayo convergente", effect: "Lanzamiento dispara 6 rayos que convergen en uno devastador.", cost: 45, source: "Drop de Moon Lord" },
  { id: "nebula-blaze", name: "Nebula Blaze", branch: "magic", tier: "endgame", signature: "Bolts caóticos", effect: "Lanzamiento alterna bolts caóticos pequeños y grandes (alta varianza).", cost: 40, source: "Fragmentos Nebula" },
  { id: "blizzard-staff", name: "Blizzard Staff", branch: "magic", tier: "endgame", signature: "Lluvia de hielo", effect: "Lanzamiento hace llover esquirlas de hielo desde arriba.", cost: 35, source: "Drop de Frost Moon" },

  // ---- Distancia (arcos) ----
  { id: "wooden-bow", name: "Wooden Bow", branch: "distance", tier: "pre", signature: "Disparo básico", effect: "Las flechas vuelan rectas; +1 flecha por disparo base.", cost: 3, source: "Crafteado con madera" },
  { id: "demon-bow", name: "Demon Bow", branch: "distance", tier: "pre", signature: "Tensado pesado", effect: "Las flechas tienen +15% knockback y perforan 1 enemigo.", cost: 8, source: "Crafteo con demonite" },
  { id: "molten-fury", name: "Molten Fury", branch: "distance", tier: "pre", signature: "Ignición de flechas", effect: "Las flechas de madera se vuelven flechas flamígeras al fuego.", cost: 12, source: "Crafteo con hellstone" },
  { id: "bee-knee", name: "The Bee's Knees", branch: "distance", tier: "hardmode", signature: "Enjambre de abejas", effect: "Las flechas generan 3-5 abejas homing al impactar.", cost: 18, source: "Drop de Queen Bee" },
  { id: "hellwing-bow", name: "Hellwing Bow", branch: "distance", tier: "hardmode", signature: "Conversión a murciélago", effect: "Las flechas de madera se convierten en murciélagos flamígeros.", cost: 20, source: "Cofres de sombra (hardmode)" },
  { id: "daedalus-stormbow", name: "Daedalus Stormbow", branch: "distance", tier: "hardmode", signature: "Lluvia del cielo", effect: "Las flechas llueven desde el cielo en lugar de volar hacia adelante.", cost: 30, source: "Drop de Hallowed Mimic" },
  { id: "ice-bow", name: "Ice Bow", branch: "distance", tier: "hardmode", signature: "Flecha de escarcha", effect: "Las flechas se vuelven virotes de hielo perforantes que ralentizan.", cost: 22, source: "Drop de Ice Mimic" },
  { id: "shadowflame-bow", name: "Shadowflame Bow", branch: "distance", tier: "hardmode", signature: "Llama de sombra", effect: "Las flechas infligen DoT de shadowflame y perforan 2.", cost: 25, source: "Drop de Goblin Summoner" },
  { id: "tsunami", name: "Tsunami", branch: "distance", tier: "endgame", signature: "Salva de 5 flechas", effect: "Dispara 5 flechas en abanico cerrado por disparo.", cost: 40, source: "Drop de Duke Fishron" },
  { id: "phantasm", name: "Phantasm", branch: "distance", tier: "endgame", signature: "Flechas fantasma", effect: "Cada golpe genera flechas fantasma extra (acumulativo).", cost: 45, source: "Fragmentos Vortex" },
  { id: "eventide", name: "Eventide", branch: "distance", tier: "endgame", signature: "Flecha convertidora", effect: "Convierte cualquier munición en una salva arcoíris de 4 flechas.", cost: 42, source: "Drop de Empress of Light" },
  { id: "aerial-bane", name: "Aerial Bane", branch: "distance", tier: "endgame", signature: "Salpicadura anti-aérea", effect: "Las flechas tienen +50% daño a enemigos voladores y salpican.", cost: 35, source: "Drop de Betsy (Ogre)" },

  // ---- Cuerpo a Cuerpo (espadas) ----
  { id: "wooden-sword", name: "Wooden Sword", branch: "melee", tier: "pre", signature: "Balanceo básico", effect: "+10% velocidad de balanceo base.", cost: 3, source: "Crafteado con madera" },
  { id: "blade-of-grass", name: "Blade of Grass", branch: "melee", tier: "pre", signature: "Filo venenoso", effect: "Los golpes infligen veneno por 5s.", cost: 10, source: "Crafteo de la jungla" },
  { id: "muramasa", name: "Muramasa", branch: "melee", tier: "pre", signature: "Auto-balanceo rápido", effect: "Habilita auto-swing; +20% velocidad de ataque.", cost: 12, source: "Mazmorra (Skeletron)" },
  { id: "phaseblade", name: "Phaseblade", branch: "melee", tier: "pre", signature: "Hoja de energía", effect: "El balanceo emite luz; +15% crítico.", cost: 10, source: "Crafteo con meteorito" },
  { id: "fiery-greatsword", name: "Fiery Greatsword", branch: "melee", tier: "pre", signature: "Fuego al golpe", effect: "Los golpes incendian enemigos (DoT).", cost: 14, source: "Crafteo con hellstone" },
  { id: "break-blade", name: "Breaker Blade", branch: "melee", tier: "hardmode", signature: "Cleave pesado", effect: "+50% tamaño, +30% knockback, arco más amplio.", cost: 20, source: "Drop de Wall of Flesh" },
  { id: "cobalt-sword", name: "Cobalt Sword", branch: "melee", tier: "hardmode", signature: "Combo de estocada", effect: "El 3er balanceo se vuelve una estocada perforante.", cost: 22, source: "Crafteo con cobalto" },
  { id: "cutlass", name: "Cutlass", branch: "melee", tier: "hardmode", signature: "Filo pirata", effect: "+10% daño, auto-swing.", cost: 20, source: "Drop de Pirate Invasion" },
  { id: "ice-sickle", name: "Ice Sickle", branch: "melee", tier: "hardmode", signature: "Onda de hoz", effect: "El balanceo emite una gran onda de escarcha perforante.", cost: 25, source: "Drop de Ice Mimic" },
  { id: "keybrand", name: "Keybrand", branch: "melee", tier: "hardmode", signature: "Golpe crítico", effect: "+25% daño crítico, +10% probabilidad de crítico.", cost: 24, source: "Mobs de mazmorra hardmode" },
  { id: "terra-blade", name: "Terra Blade", branch: "melee", tier: "hardmode", signature: "Corte de haz", effect: "El balanceo dispara un haz verde perforante.", cost: 35, source: "Crafteo True Excalibur + True Night's Edge" },
  { id: "influx-waver", name: "Influx Waver", branch: "melee", tier: "endgame", signature: "Doble corte", effect: "El balanceo dispara un haz que se divide en 2 al impactar.", cost: 40, source: "Drop de Martian Saucer" },
  { id: "horseman-blade", name: "Horseman's Blade", branch: "melee", tier: "endgame", signature: "Invocación de calabazas", effect: "El balanceo invoca calabazas flamígeras homing.", cost: 38, source: "Drop de Pumpkin Moon" },
  { id: "seedler", name: "Seedler", branch: "melee", tier: "endgame", signature: "Explosión de hojas", effect: "El balanceo genera proyectiles de hoja al impactar.", cost: 38, source: "Drop de Plantera" },
  { id: "star-wrath", name: "Star Wrath", branch: "melee", tier: "endgame", signature: "Lluvia de estrellas", effect: "El balanceo hace llover estrellas del cielo sobre el cursor.", cost: 42, source: "Drop de Moon Lord" },
  { id: "zenith", name: "Zenith", branch: "melee", tier: "endgame", signature: "Tormenta de hojas", effect: "El balanceo invoca una estela de cada espada que has sostenido.", cost: 50, source: "Crafteo (endgame)" },

  // ---- Distancia (cañones / armas de munición) ----
  { id: "flintlock-pistol", name: "Flintlock Pistol", branch: "distance", tier: "pre", signature: "Disparo rápido", effect: "+25% cadencia, -25% daño.", cost: 4, source: "Merchant (tras Skeletron)" },
  { id: "minishark", name: "Minishark", branch: "distance", tier: "pre", signature: "Rociado rápido", effect: "33% de probabilidad de no consumir munición.", cost: 12, source: "Arms Dealer (compra)" },
  { id: "musket", name: "Musket", branch: "distance", tier: "pre", signature: "Alto impacto", effect: "+50% knockback, +20% probabilidad de crítico.", cost: 8, source: "Shadow orb / crimson heart" },
  { id: "boomstick", name: "Boomstick", branch: "distance", tier: "pre", signature: "Salva de 3 perdigones", effect: "Dispara 3 perdigones en abanico.", cost: 10, source: "Jungla (subterráneo)" },
  { id: "quad-barrel", name: "Quad-Barrel Shotgun", branch: "distance", tier: "hardmode", signature: "Ráfaga de 4 perdigones", effect: "Dispara 4 perdigones; devastación a corta distancia.", cost: 20, source: "Arms Dealer (post-WoF)" },
  { id: "clockwork-assault", name: "Clockwork Assault Rifle", branch: "distance", tier: "hardmode", signature: "Ráfaga de 3 tiros", effect: "Dispara 3 balas por presión del gatillo.", cost: 22, source: "Drop de Wall of Flesh" },
  { id: "megashark", name: "Megashark", branch: "distance", tier: "hardmode", signature: "Hiperfuego", effect: "50% de probabilidad de no consumir munición; muy rápido.", cost: 28, source: "Crafteo Hallowed" },
  { id: "on-off-zero", name: "Onyx Blaster", branch: "distance", tier: "hardmode", signature: "Proyectil de ónix", effect: "Dispara un cristal de ónix perforante + 2 perdigones.", cost: 26, source: "Crafteo (post-2º orb)" },
  { id: "shotgun", name: "Shotgun", branch: "distance", tier: "hardmode", signature: "Rociado de perdigones", effect: "Dispara 4 perdigones en abanico amplio.", cost: 18, source: "Arms Dealer (hardmode)" },
  { id: "sniper-rifle", name: "Sniper Rifle", branch: "distance", tier: "hardmode", signature: "Headshot con mira", effect: "Click derecho para hacer zoom; multiplicador de crítico masivo.", cost: 30, source: "Mobs de mazmorra hardmode" },
  { id: "tactical-shotgun", name: "Tactical Shotgun", branch: "distance", tier: "hardmode", signature: "Rociado táctico", effect: "Dispara 6 perdigones ajustados; auto-fuego.", cost: 26, source: "Mobs de mazmorra hardmode" },
  { id: "chain-gun", name: "Chain Gun", branch: "distance", tier: "endgame", signature: "Manguera de balas", effect: "Cadencia insana; el spread es alto.", cost: 38, source: "Drop de Frost Moon" },
  { id: "s-d-m-g", name: "SDMG", branch: "distance", tier: "endgame", signature: "Space Dolphin", effect: "+15% daño, +5% crítico, rápido.", cost: 45, source: "Drop de Moon Lord" },
  { id: "phantasm-blast", name: "Vortex Beater", branch: "distance", tier: "endgame", signature: "Cohete alt", effect: "Las balas homing; alt-fuego dispara un cohete.", cost: 45, source: "Fragmentos Vortex" },
  { id: "xenopopper", name: "Xenopopper", branch: "distance", tier: "endgame", signature: "Estallido de burbuja", effect: "Dispara burbujas que estallan en balas homing.", cost: 40, source: "Drop de Martian Saucer" },
  { id: "snowman-cannon", name: "Snowman Cannon", branch: "distance", tier: "endgame", signature: "Cohete homing", effect: "Dispara cohetes homing que rastrean enemigos.", cost: 38, source: "Drop de Frost Moon" },
];

export function getCodexForBranch(cls: BranchId): CodexWeapon[] {
  return CODEX.filter((c) => c.branch === cls);
}

/** @deprecated Use `getCodexForBranch`. Kept for backward-compat with build-share codec. */
export const getCodexForClass = getCodexForBranch;

// Rune slot count grows with weapon level (Memory Codex branch investment)
export function runeSlotsForLevel(level: number): number {
  if (level < 50) return 0;
  if (level < 75) return 1;
  if (level < 100) return 2;
  if (level < 125) return 3;
  if (level < 150) return 4;
  return 5; // capped at 5 for balance
}
