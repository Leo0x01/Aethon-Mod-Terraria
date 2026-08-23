import type { WeaponId } from "@/lib/mod-data";

/**
 * Curated build presets — one-click example builds that showcase the skill-tree
 * system. Each preset specifies a weapon, a fixed seed, and a set of allocated
 * node indices (into the weapon's skillTree array). Encoded as v1 build hashes
 * so they load via the existing build-import infrastructure.
 */

export interface BuildPreset {
  id: string;
  name: string;
  archetype: string; // e.g. "Glass Cannon", "Tank"
  weaponId: WeaponId;
  seed: number;
  /** Indices into the weapon's skillTree array. */
  nodeIndices: number[];
  description: string;
  difficulty: "easy" | "medium" | "hard";
  accent: string; // hex color for the preset card
}

export const BUILD_PRESETS: BuildPreset[] = [
  {
    id: "arcane-storm",
    name: "Arcane Storm",
    archetype: "Glass Cannon",
    weaponId: "book",
    seed: 1337,
    // Mana Flow (0-4) + Projectile Evolution (10-14) + Cosmic Spells (20-24)
    nodeIndices: [0, 1, 2, 3, 4, 10, 11, 12, 13, 14, 20, 21, 22, 23, 24],
    description:
      "Maximum magical carnage. Bottomless mana, splitting homing bolts, and Black Holes pulling enemies into Supernovas. Dies if sneezed on.",
    difficulty: "hard",
    accent: "#b388ff",
  },
  {
    id: "eternal-sustainer",
    name: "Eternal Sustainer",
    archetype: "Lifesteal Tank",
    weaponId: "book",
    seed: 4242,
    // Mana Flow (0-4) + Arcane Conversion (15-19)
    nodeIndices: [0, 1, 2, 3, 4, 15, 16, 17, 18, 19],
    description:
      "Mana shield + lifesteal + desperation scaling. The lower your mana, the harder you hit. Nearly unkillable once online.",
    difficulty: "medium",
    accent: "#3dd6c4",
  },
  {
    id: "starfall-marksman",
    name: "Starfall Marksman",
    archetype: "Ranged DPS",
    weaponId: "bow",
    seed: 2718,
    // Quiver Mastery (5-9) + Celestial Shots (15-19)
    nodeIndices: [5, 6, 7, 8, 9, 15, 16, 17, 18, 19],
    description:
      "Endless Quiver + Starfall Storm + Supernova charged shots. Rain cosmic death from range while never running dry.",
    difficulty: "easy",
    accent: "#f5c451",
  },
  {
    id: "dawnbreaker",
    name: "Dawnbreaker",
    archetype: "Combo Brawler",
    weaponId: "sword",
    seed: 1618,
    // Combo Mastery (5-9) + Solar Wrath (10-14)
    nodeIndices: [5, 6, 7, 8, 9, 10, 11, 12, 13, 14],
    description:
      "Escalating combo meter into a Supernova Strike finisher. Solar aura burns everything around you while you dance through enemies.",
    difficulty: "medium",
    accent: "#ff9a3c",
  },
  {
    id: "bulwark-of-dawn",
    name: "Bulwark of Dawn",
    archetype: "Parry Tank",
    weaponId: "sword",
    seed: 9090,
    // Aegis of Dawn (15-19) + Blade Genesis (0-4)
    nodeIndices: [0, 1, 2, 3, 4, 15, 16, 17, 18, 19],
    description:
      "Parry frames, damage reflection, invuln dashes. Become untouchable — every blocked projectile fires back at +50% damage.",
    difficulty: "hard",
    accent: "#3dd6c4",
  },
  {
    id: "void-artillery",
    name: "Void Artillery",
    archetype: "Heavy Ordnance",
    weaponId: "cannon",
    seed: 3141,
    // Heavy Ordnance (15-19) + Slug Genesis (0-4)
    nodeIndices: [0, 1, 2, 3, 4, 15, 16, 17, 18, 19],
    description:
      "Charged shots, artillery mode, orbital strikes. Deploy, overcharge, and call satellite beams from above.",
    difficulty: "medium",
    accent: "#3dd6c4",
  },
];
