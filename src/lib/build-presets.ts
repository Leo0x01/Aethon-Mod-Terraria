import type { BranchId } from "@/lib/mod-data";

/**
 * Curated build presets — one-click example builds that showcase the skill-tree
 * system. Each preset specifies a branch, a fixed seed, and a set of allocated
 * node indices (into the branch's skillTree array). Encoded as v1 build hashes
 * so they load via the existing build-import infrastructure.
 */

export interface BuildPreset {
  id: string;
  name: string;
  archetype: string; // e.g. "Glass Cannon", "Tank"
  branchId: BranchId;
  seed: number;
  /** Indices into the branch's skillTree array. */
  nodeIndices: number[];
  description: string;
  difficulty: "easy" | "medium" | "hard";
  accent: string; // hex color for the preset card
}

export const BUILD_PRESETS: BuildPreset[] = [
  {
    id: "arcane-storm",
    name: "Tormenta Arcana",
    archetype: "Glass Cannon",
    branchId: "magic",
    seed: 1337,
    // Flujo de Maná (0-4) + Evolución de Proyectiles (10-14) + Hechizos Cósmicos (20-24)
    nodeIndices: [0, 1, 2, 3, 4, 10, 11, 12, 13, 14, 20, 21, 22, 23, 24],
    description:
      "Carnage mágico máximo. Maná inagotable, bolts homing que se dividen, y Agujeros Negros atrayendo enemigos hacia Supernovas. Muere si le estornudan.",
    difficulty: "hard",
    accent: "#b388ff",
  },
  {
    id: "eternal-sustainer",
    name: "Sustentador Eterno",
    archetype: "Lifesteal Tank",
    branchId: "magic",
    seed: 4242,
    // Flujo de Maná (0-4) + Conversión Arcana (15-19)
    nodeIndices: [0, 1, 2, 3, 4, 15, 16, 17, 18, 19],
    description:
      "Escudo de maná + lifesteal + escalado por desesperación. Cuanto menor tu maná, más duro golpeas. Casi inmortal una vez armado.",
    difficulty: "medium",
    accent: "#3dd6c4",
  },
  {
    id: "starfall-marksman",
    name: "Tirador de la Lluvia Estelar",
    archetype: "Ranged DPS",
    branchId: "distance",
    seed: 2718,
    // Maestría de Carcaj (5-9) + Disparos Celestiales (15-19)
    nodeIndices: [5, 6, 7, 8, 9, 15, 16, 17, 18, 19],
    description:
      "Carcaj infinito + Tormenta de estrellas + Supernova cargada. Llueve muerte cósmica desde lejos sin quedarte sin munición.",
    difficulty: "easy",
    accent: "#f5c451",
  },
  {
    id: "dawnbreaker",
    name: "Rompealbas",
    archetype: "Combo Brawler",
    branchId: "melee",
    seed: 1618,
    // Maestría de Combo (5-9) + Ira Solar (10-14)
    nodeIndices: [5, 6, 7, 8, 9, 10, 11, 12, 13, 14],
    description:
      "Medidor de combo escalando hacia un remate de Golpe de supernova. Aura solar quema todo a tu alrededor mientras danzas entre enemigos.",
    difficulty: "medium",
    accent: "#ff9a3c",
  },
  {
    id: "bulwark-of-dawn",
    name: "Baluarte del Alba",
    archetype: "Parry Tank",
    branchId: "melee",
    seed: 9090,
    // Égida del Alba (15-19) + Génesis de Hoja (0-4)
    nodeIndices: [0, 1, 2, 3, 4, 15, 16, 17, 18, 19],
    description:
      "Frames de parry, reflexión de daño, dashes invulnerables. Vuelve intocable — cada proyectil bloqueado rebota con +50% daño.",
    difficulty: "hard",
    accent: "#3dd6c4",
  },
  {
    id: "void-artillery",
    name: "Artillería del Vacío",
    archetype: "Heavy Ordnance",
    branchId: "distance",
    seed: 3141,
    // Génesis de Proyectiles (0-4) + Disparos Celestiales (15-19)
    nodeIndices: [0, 1, 2, 3, 4, 15, 16, 17, 18, 19],
    description:
      "Disparos cargados, salva de meteoros, supernovas. Dispara virotes de vacío y haz que el cielo llueva fuego cósmico.",
    difficulty: "medium",
    accent: "#3dd6c4",
  },
];

// ---------------------------------------------------------------------------
// User-saved presets — stored in localStorage as a JSON array of {id, name, hash, createdAt}.
// The hash is a v1/v2 build-share hash (encodes branch + seed + nodes + codex).
// ---------------------------------------------------------------------------

export interface UserPreset {
  id: string;
  name: string;
  hash: string; // encoded build hash (v1 or v2)
  createdAt: number; // epoch ms
}

const USER_PRESETS_KEY = "aethon:user-presets";

export function loadUserPresets(): UserPreset[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = window.localStorage.getItem(USER_PRESETS_KEY);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];
    return parsed.filter(
      (p): p is UserPreset =>
        p &&
        typeof p.id === "string" &&
        typeof p.name === "string" &&
        typeof p.hash === "string" &&
        typeof p.createdAt === "number",
    );
  } catch {
    return [];
  }
}

export function saveUserPreset(name: string, hash: string): UserPreset {
  const presets = loadUserPresets();
  const preset: UserPreset = {
    id: `user-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
    name: name.slice(0, 40) || "Mi build",
    hash,
    createdAt: Date.now(),
  };
  presets.unshift(preset);
  try {
    window.localStorage.setItem(USER_PRESETS_KEY, JSON.stringify(presets));
  } catch {
    // storage full or disabled — silently ignore
  }
  return preset;
}

export function deleteUserPreset(id: string): void {
  const presets = loadUserPresets().filter((p) => p.id !== id);
  try {
    window.localStorage.setItem(USER_PRESETS_KEY, JSON.stringify(presets));
  } catch {
    // ignore
  }
}
