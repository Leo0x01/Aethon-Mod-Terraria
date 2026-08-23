import type { WeaponId } from "@/lib/mod-data";
import { CODEX } from "@/lib/mod-data";

/**
 * Build share codec — encodes/decodes a skill-tree build (and optionally the
 * Memory Codex rune loadout) to a compact URL hash.
 *
 * Formats (URL hash, after '#'):
 *   v1.<weaponId>.<seed>.<nodeIndices>          (skill tree only — backward compat)
 *   v2.<weaponId>.<seed>.<nodeIndices>.<codexIndices>
 *
 * Where:
 *   - weaponId: 1 char (b=bow, s=sword, c=cannon, m=book / magic)
 *   - seed: base36 number
 *   - nodeIndices: comma-separated indices into the weapon's skillTree (base36)
 *   - codexIndices (v2 only): comma-separated indices into the CODEX array
 *     filtered to the weapon's class (base36). Represents memorized rune IDs.
 *
 * v1 hashes still decode (codexIndices defaults to []). v2 is emitted when the
 * codex loadout is non-empty.
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
  codexIndices: number[]; // indices into the class-filtered CODEX array
}

function encodeIndices(allocatedIds: string[], allIds: string[]): string {
  const indices = allocatedIds
    .map((id) => allIds.indexOf(id))
    .filter((i) => i >= 0)
    .sort((a, b) => a - b);
  return indices.map((i) => i.toString(36)).join(",");
}

function decodeIndices(
  str: string,
  maxLen: number,
): number[] {
  return str
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean)
    .map((s) => parseInt(s, 36))
    .filter((i) => !Number.isNaN(i) && i >= 0 && i < maxLen);
}

/** Codex IDs for a given weapon class (in CODEX array order). */
function codexIdsForClass(cls: WeaponId): string[] {
  return CODEX.filter((c) => c.class === cls).map((c) => c.id);
}

export function encodeBuild(
  weaponId: WeaponId,
  seed: number,
  allocatedNodeIds: string[],
  allNodeIds: string[],
  codexIds?: string[],
): string {
  const nodeStr = encodeIndices(allocatedNodeIds, allNodeIds);
  // v2 only if codex loadout is provided and non-empty.
  if (codexIds && codexIds.length > 0) {
    const classCodexIds = codexIdsForClass(weaponId);
    const codexStr = encodeIndices(codexIds, classCodexIds);
    return `v2.${WEAPON_CODE[weaponId]}.${seed.toString(36)}.${nodeStr}.${codexStr}`;
  }
  return `v1.${WEAPON_CODE[weaponId]}.${seed.toString(36)}.${nodeStr}`;
}

export function decodeBuild(hash: string, allNodeIds: string[]): SharedBuild | null {
  const raw = hash.replace(/^#/, "").trim();
  const isV2 = raw.startsWith("v2.");
  const isV1 = raw.startsWith("v1.");
  if (!isV1 && !isV2) return null;
  const parts = raw.slice(3).split(".");
  if (parts.length < 3) return null;
  const [wCode, seedStr, idxStr, codexStr] = parts;
  const weaponId = WEAPON_FROM_CODE[wCode];
  if (!weaponId) return null;
  const seed = parseInt(seedStr, 36);
  if (Number.isNaN(seed)) return null;
  const nodeIndices = decodeIndices(idxStr, allNodeIds.length);
  let codexIndices: number[] = [];
  if (isV2 && codexStr !== undefined) {
    const classCodexIds = codexIdsForClass(weaponId);
    codexIndices = decodeIndices(codexStr, classCodexIds.length);
  }
  return { weaponId, seed, nodeIndices, codexIndices };
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

/** Resolve codex indices → codex IDs for a given weapon class. */
export function codexIdsFromIndices(
  cls: WeaponId,
  indices: number[],
): string[] {
  const classCodexIds = codexIdsForClass(cls);
  return indices
    .map((i) => classCodexIds[i])
    .filter((id): id is string => Boolean(id));
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
