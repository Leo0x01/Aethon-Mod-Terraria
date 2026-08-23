import type { WeaponId } from "@/lib/mod-data";

/**
 * Build share codec — encodes/decodes a skill-tree build to a compact URL hash.
 *
 * Format (URL hash, after '#'):
 *   v1.<weaponId>.<seed>.<nodeIds>
 *
 * Where:
 *   - weaponId: 1 char (b=bow, s=sword, c=cannon, m=book / magic)
 *   - seed: base36 number
 *   - nodeIds: comma-separated short indices into the weapon's skillTree (base36)
 *
 * This is a compact, human-unreadable but tiny representation. We keep it simple
 * and versioned (v1) so the format can evolve.
 */

const WEAPON_CODE: Record<WeaponId, string> = {
  bow: "b",
  sword: "s",
  cannon: "c",
  book: "m",
};
const WEAPON_FROM_CODE: Record<string, WeaponId> = {
  b: "bow",
  s: "sword",
  c: "cannon",
  m: "book",
};

export interface SharedBuild {
  weaponId: WeaponId;
  seed: number;
  nodeIndices: number[]; // indices into the weapon's skillTree array
}

export function encodeBuild(
  weaponId: WeaponId,
  seed: number,
  allocatedNodeIds: string[],
  allNodeIds: string[],
): string {
  const indices = allocatedNodeIds
    .map((id) => allNodeIds.indexOf(id))
    .filter((i) => i >= 0)
    .sort((a, b) => a - b);
  const idxStr = indices
    .map((i) => i.toString(36))
    .join(",");
  return `v1.${WEAPON_CODE[weaponId]}.${seed.toString(36)}.${idxStr}`;
}

export function decodeBuild(hash: string, allNodeIds: string[]): SharedBuild | null {
  const raw = hash.replace(/^#/, "").trim();
  if (!raw.startsWith("v1.")) return null;
  const parts = raw.slice(3).split(".");
  if (parts.length < 3) return null;
  const [wCode, seedStr, idxStr] = parts;
  const weaponId = WEAPON_FROM_CODE[wCode];
  if (!weaponId) return null;
  const seed = parseInt(seedStr, 36);
  if (Number.isNaN(seed)) return null;
  const nodeIndices = idxStr
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean)
    .map((s) => parseInt(s, 36))
    .filter((i) => !Number.isNaN(i) && i >= 0 && i < allNodeIds.length);
  return { weaponId, seed, nodeIndices };
}

/** Read the build hash from the current URL (without the leading '#'). */
export function readHashBuild(allNodeIds: string[]): SharedBuild | null {
  if (typeof window === "undefined") return null;
  const h = window.location.hash;
  if (!h) return null;
  return decodeBuild(h, allNodeIds);
}

/** Write a build into the URL hash (replaces history state, no scroll jump). */
export function writeHashBuild(encoded: string) {
  if (typeof window === "undefined") return;
  // Replace state to avoid polluting back-button history on every toggle.
  const newUrl = `${window.location.pathname}#${encoded}`;
  window.history.replaceState(null, "", newUrl);
}

/** Clear the build hash. */
export function clearHashBuild() {
  if (typeof window === "undefined") return;
  window.history.replaceState(null, "", window.location.pathname);
}

/**
 * Copy a share URL (with hash) to clipboard. Returns true on success.
 * Uses the visible origin + path so the link is directly shareable.
 */
export async function copyShareUrl(encoded: string): Promise<boolean> {
  if (typeof window === "undefined") return false;
  const url = `${window.location.origin}${window.location.pathname}#${encoded}`;
  try {
    await navigator.clipboard.writeText(url);
    return true;
  } catch {
    // Fallback for non-secure contexts / older browsers
    try {
      const ta = document.createElement("textarea");
      ta.value = url;
      ta.style.position = "fixed";
      ta.style.opacity = "0";
      document.body.appendChild(ta);
      ta.select();
      const ok = document.execCommand("copy");
      document.body.removeChild(ta);
      return ok;
    } catch {
      return false;
    }
  }
}
