"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import {
  cumulativeSkillPoints,
  WEAPONS,
  type SkillNode,
  type WeaponId,
} from "@/lib/mod-data";
import {
  codexIdsFromIndices,
  copyShareUrl,
  decodeBuild,
  encodeBuild,
  writeHashBuild,
} from "@/lib/build-share";
import { loadString, removeKey, saveString } from "@/lib/storage";
import { SectionHeading } from "./lore-section";
import { cn } from "@/lib/utils";

const RARITY_STYLE: Record<
  SkillNode["rarity"],
  { fill: string; ring: string; glow: string; label: string }
> = {
  common: {
    fill: "rgba(180,180,210,0.35)",
    ring: "rgba(200,200,230,0.7)",
    glow: "rgba(200,200,230,0.35)",
    label: "Common",
  },
  rare: {
    fill: "rgba(120,170,255,0.4)",
    ring: "rgba(140,180,255,0.95)",
    glow: "rgba(120,170,255,0.6)",
    label: "Rare",
  },
  legendary: {
    fill: "rgba(245,196,81,0.45)",
    ring: "rgba(255,220,140,1)",
    glow: "rgba(245,196,81,0.85)",
    label: "Legendary",
  },
};

/**
 * Deterministic pseudo-random from a seed (mulberry32).
 */
function mulberry32(seed: number) {
  return function () {
    let t = (seed += 0x6d2b79f5);
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

interface PlacedNode extends SkillNode {
  px: number; // pixel coords
  py: number;
}

function readInitialBuild(): {
  weaponId: WeaponId;
  seed: number;
  allocated: Set<string>;
} {
  if (typeof window === "undefined") {
    return {
      weaponId: "book" as WeaponId,
      seed: 1337,
      allocated: new Set<string>(),
    };
  }
  // Decode using the weapon encoded IN the hash (its node list). We don't
  // need to probe every weapon — the hash itself declares which weapon it's for.
  const hash = window.location.hash;
  if (!hash || !hash.startsWith("#v1.")) {
    return {
      weaponId: "book" as WeaponId,
      seed: 1337,
      allocated: new Set<string>(),
    };
  }
  // Peek the weapon code to find the matching weapon definition.
  const parts = hash.slice(4).split(".");
  const wCode = parts[0];
  const WEAPON_FROM_CODE: Record<string, WeaponId> = {
    b: "bow",
    s: "sword",
    c: "cannon",
    m: "book",
  };
  const wid = WEAPON_FROM_CODE[wCode];
  if (!wid) {
    return {
      weaponId: "book" as WeaponId,
      seed: 1337,
      allocated: new Set<string>(),
    };
  }
  const w = WEAPONS.find((x) => x.id === wid)!;
  const ids = w.skillTree.map((n) => n.id);
  const shared = decodeBuild(hash, ids);
  if (!shared) {
    return {
      weaponId: "book" as WeaponId,
      seed: 1337,
      allocated: new Set<string>(),
    };
  }
  const alloc = new Set<string>();
  for (const idx of shared.nodeIndices) {
    const id = w.skillTree[idx]?.id;
    if (id) alloc.add(id);
  }
  return {
    weaponId: shared.weaponId,
    seed: shared.seed,
    allocated: alloc,
  };
}

export function SkillTreeView() {
  // SSR-safe defaults; the shared build is restored in a mount effect below.
  const [weaponId, setWeaponId] = useState<WeaponId>("book");
  const [seed, setSeed] = useState(1337);
  const [allocated, setAllocated] = useState<Set<string>>(new Set());
  const [selected, setSelected] = useState<string | null>(null);
  const [hydrated, setHydrated] = useState(false);
  // Hovered node for the floating tooltip overlay (HTML, so text wraps nicely).
  const [hovered, setHovered] = useState<string | null>(null);

  // Node search: filters/highlights nodes in the constellation by name.
  const [nodeQuery, setNodeQuery] = useState("");
  const searchInputRef = useRef<HTMLInputElement | null>(null);

  // Build import panel state (declared early so the keyboard handler can close it).
  const [showImport, setShowImport] = useState(false);
  const [importValue, setImportValue] = useState("");
  const [importStatus, setImportStatus] = useState<
    "idle" | "ok" | "error"
  >("idle");

  // Build comparison: snapshot "build A" to diff against the current build.
  // Stored as a serialized hash string; persisted to localStorage so it
  // survives reloads. null = no snapshot saved.
  const SNAPSHOT_KEY = "aethon:build-snapshot-a";
  const [snapshotA, setSnapshotA] = useState<string | null>(null);

  const weapon = WEAPONS.find((w) => w.id === weaponId)!;

  // Procedurally (re)place nodes using the seed → jitter + occasional extra branch arcs
  const placed = useMemo<PlacedNode[]>(() => {
    const rng = mulberry32(seed);
    const W = 720;
    const H = 620;
    const cx = W / 2;
    const cy = H / 2;
    return weapon.skillTree.map((n) => {
      const jx = (rng() - 0.5) * 0.08;
      const jy = (rng() - 0.5) * 0.08;
      const px = (n.x + jx) * W;
      const py = (n.y + jy) * H;
      // nudge toward center if too far out
      const dx = px - cx;
      const dy = py - cy;
      const dist = Math.hypot(dx, dy);
      const maxR = Math.min(W, H) / 2 - 40;
      let fx = px;
      let fy = py;
      if (dist > maxR) {
        fx = cx + (dx / dist) * maxR;
        fy = cy + (dy / dist) * maxR;
      }
      return { ...n, px: fx, py: fy };
    });
  }, [weapon, seed]);

  // reset allocations when weapon changes
  const onWeaponChange = (id: WeaponId) => {
    setWeaponId(id);
    setAllocated(new Set());
    setSelected(null);
  };

  const onReroll = () => {
    setSeed(Math.floor(Math.random() * 1_000_000));
    setAllocated(new Set());
    setSelected(null);
  };

  // available points = a demo level (say 60) → cumulative
  const demoLevel = 60;
  const totalPoints = cumulativeSkillPoints(demoLevel);
  const spent = useMemo(
    () =>
      placed
        .filter((n) => allocated.has(n.id))
        .reduce((sum, n) => sum + n.cost, 0),
    [allocated, placed],
  );
  const available = totalPoints - spent;

  // All node ids (in tree order) — used by build-share codec.
  const allNodeIds = useMemo(() => weapon.skillTree.map((n) => n.id), [weapon]);

  // Restore a shared build from the URL hash ONCE on mount (client-only,
  // so SSR + hydration match — server renders empty defaults). This is a
  // legitimate one-time hydration of browser-only state.
  useEffect(() => {
    /* eslint-disable react-hooks/set-state-in-effect */
    const restored = readInitialBuild();
    setWeaponId(restored.weaponId);
    setSeed(restored.seed);
    setAllocated(restored.allocated);
    // Restore the build-compare snapshot from localStorage (if any).
    setSnapshotA(loadString(SNAPSHOT_KEY));
    setHydrated(true);
    /* eslint-enable react-hooks/set-state-in-effect */
  }, []);

  // Global keyboard shortcuts:
  //  - "/" focuses the node search box (unless already typing in an input)
  //  - "Escape" blurs the focused node / clears the search / closes the import panel
  useEffect(() => {
    const onKeyDown = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement | null;
      const isTyping =
        target &&
        (target.tagName === "INPUT" ||
          target.tagName === "TEXTAREA" ||
          target.isContentEditable);

      if (e.key === "/" && !isTyping) {
        // Only act when the skill-tree section is in view.
        const section = document.querySelector("#skill-tree");
        if (!section) return;
        const rect = section.getBoundingClientRect();
        if (rect.top < window.innerHeight && rect.bottom > 0) {
          e.preventDefault();
          searchInputRef.current?.focus();
        }
      } else if (e.key === "Escape") {
        if (isTyping) {
          // Clear the focused input's value if it's our search box.
          if (target?.getAttribute("aria-label") === "Buscar nodo por nombre") {
            setNodeQuery("");
            (target as HTMLInputElement).blur();
          }
        } else {
          // When not typing, Escape clears any active search query + closes
          // the import panel + tooltips (if the skill-tree is in view).
          const section = document.querySelector("#skill-tree");
          if (section) {
            const rect = section.getBoundingClientRect();
            if (rect.top < window.innerHeight && rect.bottom > 0) {
              if (nodeQuery) setNodeQuery("");
              setShowImport(false);
              setHovered(null);
            }
          }
        }
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [nodeQuery]);

  // Persist current build to URL hash — but only AFTER hydration so we don't
  // overwrite the shared hash we just restored with empty defaults. Preserves
  // any codex loadout already in the hash so the two features coexist.
  useEffect(() => {
    if (!hydrated) return;
    // Read existing codex segment from the hash (if any) to preserve it.
    let codexIds: string[] | undefined;
    const existingHash =
      typeof window !== "undefined" ? window.location.hash : "";
    if (existingHash) {
      const shared = decodeBuild(existingHash, allNodeIds);
      if (shared && shared.codexIndices.length > 0) {
        codexIds = codexIdsFromIndices(
          shared.weaponId,
          shared.codexIndices,
        );
      }
    }
    const enc = encodeBuild(
      weaponId,
      seed,
      [...allocated],
      allNodeIds,
      codexIds,
    );
    writeHashBuild(enc);
  }, [allocated, weaponId, seed, allNodeIds, hydrated]);

  const [shareStatus, setShareStatus] = useState<
    "idle" | "copied" | "error"
  >("idle");
  const onShare = async () => {
    // Preserve codex loadout from the existing hash so sharing includes it.
    let codexIds: string[] | undefined;
    const existingHash = window.location.hash;
    if (existingHash) {
      const shared = decodeBuild(existingHash, allNodeIds);
      if (shared && shared.codexIndices.length > 0) {
        codexIds = codexIdsFromIndices(shared.weaponId, shared.codexIndices);
      }
    }
    const enc = encodeBuild(
      weaponId,
      seed,
      [...allocated],
      allNodeIds,
      codexIds,
    );
    writeHashBuild(enc);
    const ok = await copyShareUrl(enc);
    setShareStatus(ok ? "copied" : "error");
    window.setTimeout(() => setShareStatus("idle"), 2200);
  };

  // Build import: paste a share URL or hash to load someone else's build.
  const onImport = () => {
    // Accept either a full URL (...#v1.x.y.z / ...#v2.x.y.z.w) or a bare hash.
    const raw = importValue.trim();
    if (!raw) return;
    let hash = "";
    const v1Idx = raw.indexOf("#v1.");
    const v2Idx = raw.indexOf("#v2.");
    if (v1Idx >= 0) hash = raw.slice(v1Idx);
    else if (v2Idx >= 0) hash = raw.slice(v2Idx);
    else if (raw.startsWith("v1.") || raw.startsWith("v2.")) hash = `#${raw}`;
    if (!hash) {
      setImportStatus("error");
      window.setTimeout(() => setImportStatus("idle"), 2400);
      return;
    }
    // Decode against every weapon to find the matching one.
    for (const w of WEAPONS) {
      const ids = w.skillTree.map((n) => n.id);
      const shared = decodeBuild(hash, ids);
      if (shared && shared.weaponId === w.id) {
        const alloc = new Set<string>();
        for (const idx of shared.nodeIndices) {
          const id = w.skillTree[idx]?.id;
          if (id) alloc.add(id);
        }
        setWeaponId(shared.weaponId);
        setSeed(shared.seed);
        setAllocated(alloc);
        setSelected(null);
        writeHashBuild(hash);
        // Notify the Memory Codex to sync its class + loadout to the imported build.
        window.dispatchEvent(
          new CustomEvent("aethon:build-imported", {
            detail: { hash },
          }),
        );
        setImportStatus("ok");
        setImportValue("");
        setShowImport(false);
        window.setTimeout(() => setImportStatus("idle"), 2400);
        return;
      }
    }
    setImportStatus("error");
    window.setTimeout(() => setImportStatus("idle"), 2400);
  };

  const onRandomize = () => {
    // Greedy random allocation: pick random valid nodes until we run out of
    // points or all eligible nodes are taken. Respects prereqs + budget.
    const rng = mulberry32(Math.floor(Math.random() * 1_000_000));
    const next = new Set<string>();
    let remaining = totalPoints;
    const order = [...weapon.skillTree];
    const maxPasses = order.length;
    for (let pass = 0; pass < maxPasses; pass++) {
      const pool = [...order]
        .map((n) => ({ n, r: rng() }))
        .sort((a, b) => a.r - b.r)
        .map((x) => x.n);
      let progressed = false;
      for (const n of pool) {
        if (next.has(n.id)) continue;
        if (n.cost > remaining) continue;
        if (n.prereq && !next.has(n.prereq)) continue;
        // 70% chance to take each eligible node → organic, not full, builds
        if (rng() < 0.7) {
          next.add(n.id);
          remaining -= n.cost;
          progressed = true;
        }
      }
      if (!progressed || remaining <= 0) break;
    }
    setAllocated(next);
    setSelected(null);
  };

  const canAllocate = (n: PlacedNode) => {
    if (allocated.has(n.id)) return false;
    if (available < n.cost) return false;
    if (n.prereq && !allocated.has(n.prereq)) return false;
    return true;
  };

  const toggle = (n: PlacedNode) => {
    setSelected(n.id);
    if (!canAllocate(n) && !allocated.has(n.id)) return;
    setAllocated((prev) => {
      const next = new Set(prev);
      if (next.has(n.id)) {
        // deallocate only if nothing depends on it
        const hasDep = placed.some((p) => p.prereq === n.id && next.has(p.id));
        if (hasDep) return next;
        next.delete(n.id);
      } else {
        next.add(n.id);
      }
      return next;
    });
  };

  const sel = placed.find((n) => n.id === selected) ?? null;

  return (
    <section id="skill-tree" className="relative py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <SectionHeading
          kicker="Árbol de Habilidades"
          title={
            <>
              Constelaciones{" "}
              <span className="text-glow-violet">procedurales</span>
            </>
          }
          subtitle="Cada arma genera su propio árbol con un seed. Las ramas irradian como estrellas conectadas por puentes de luz. Haz clic en un nodo para asignarlo; respeta prerrequisitos y tu presupuesto de puntos."
        />

        <div className="mt-10 flex flex-wrap items-center justify-center gap-2">
          {WEAPONS.map((w) => (
            <button
              key={w.id}
              onClick={() => onWeaponChange(w.id)}
              className={cn(
                "flex items-center gap-2 rounded-full border px-4 py-2 text-sm transition",
                weaponId === w.id
                  ? "border-transparent"
                  : "border-border/60 text-muted-foreground hover:text-foreground",
              )}
              style={
                weaponId === w.id
                  ? {
                      borderColor: w.accent,
                      color: w.accent,
                      boxShadow: `0 0 20px -6px ${w.accent}88`,
                    }
                  : undefined
              }
            >
              <span
                className="h-2 w-2 rounded-full"
                style={{ background: w.accent }}
              />
              {w.name}
            </button>
          ))}
        </div>

        <div className="mt-8 grid grid-cols-1 gap-6 lg:grid-cols-[1.4fr_0.6fr]">
          {/* CANVAS */}
          <div className="relative overflow-hidden rounded-3xl border border-border/60 bg-card/30 p-2">
            <div className="pointer-events-none absolute inset-0 bg-cosmic-grid opacity-25" />
            <div className="pointer-events-none absolute inset-0">
              <div className="absolute left-1/2 top-1/2 h-40 w-40 -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary/10 blur-3xl" />
            </div>

            {/* node search (top-left) */}
            <div className="absolute left-3 top-3 z-10 flex items-center gap-2">
              <div className="relative">
                <span className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground">
                  ⌕
                </span>
                <input
                  ref={searchInputRef}
                  value={nodeQuery}
                  onChange={(e) => setNodeQuery(e.target.value)}
                  placeholder="buscar nodo…  ( / )"
                  className="w-36 rounded-full border border-border/60 bg-card/80 py-1.5 pl-7 pr-7 text-xs text-foreground backdrop-blur placeholder:text-muted-foreground/60 focus:border-primary/50 focus:outline-none focus:ring-1 focus:ring-primary/30 sm:w-48"
                  aria-label="Buscar nodo por nombre"
                />
                {nodeQuery && (
                  <button
                    onClick={() => setNodeQuery("")}
                    className="absolute right-1.5 top-1/2 -translate-y-1/2 text-muted-foreground transition hover:text-foreground"
                    aria-label="Limpiar búsqueda"
                  >
                    ✕
                  </button>
                )}
              </div>
              {nodeQuery && (
                <span className="hidden rounded-full bg-primary/10 px-2 py-1 font-mono text-[10px] text-primary sm:inline">
                  {placed.filter((n) =>
                    n.name.toLowerCase().includes(nodeQuery.toLowerCase()),
                  ).length}{" "}
                  / {placed.length}
                </span>
              )}
            </div>

            {/* controls */}
            <div className="absolute right-3 top-3 z-10 flex flex-wrap justify-end gap-2">
              <button
                onClick={onReroll}
                className="rounded-full border border-border/60 bg-card/80 px-3 py-1.5 text-xs text-muted-foreground backdrop-blur transition hover:border-primary/50 hover:text-primary"
              >
                ↻ re-generar (seed {seed.toString(36)})
              </button>
              <button
                onClick={onRandomize}
                className="rounded-full border border-accent/40 bg-accent/10 px-3 py-1.5 text-xs text-accent backdrop-blur transition hover:border-accent hover:bg-accent/20"
              >
                🎲 build aleatorio
              </button>
              <button
                onClick={onShare}
                className="rounded-full border border-primary/40 bg-primary/10 px-3 py-1.5 text-xs text-primary backdrop-blur transition hover:border-primary hover:bg-primary/20"
              >
                {shareStatus === "copied"
                  ? "✓ enlace copiado"
                  : shareStatus === "error"
                    ? "✕ error"
                    : "↗ compartir build"}
              </button>
              <button
                onClick={() => setShowImport((s) => !s)}
                className={cn(
                  "rounded-full border px-3 py-1.5 text-xs backdrop-blur transition",
                  showImport
                    ? "border-accent bg-accent/20 text-accent"
                    : "border-border/60 bg-card/80 text-muted-foreground hover:border-accent/50 hover:text-accent",
                )}
              >
                {importStatus === "ok"
                  ? "✓ importado"
                  : importStatus === "error"
                    ? "✕ inválido"
                    : "⤓ importar"}
              </button>
            </div>

            {/* import panel (collapsible) */}
            <AnimatePresence>
              {showImport && (
                <motion.div
                  initial={{ opacity: 0, height: 0 }}
                  animate={{ opacity: 1, height: "auto" }}
                  exit={{ opacity: 0, height: 0 }}
                  transition={{ duration: 0.2 }}
                  className="overflow-hidden"
                >
                  <div className="mx-3 mt-3 flex flex-col gap-2 rounded-2xl border border-border/60 bg-card/80 p-3 backdrop-blur sm:flex-row sm:items-center">
                    <input
                      value={importValue}
                      onChange={(e) => setImportValue(e.target.value)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") onImport();
                      }}
                      placeholder="Pega un enlace o hash (#v1.m.1b9.0,1,2…)"
                      className="w-full flex-1 rounded-full border border-border/60 bg-background/60 px-4 py-2 text-xs text-foreground placeholder:text-muted-foreground/60 focus:border-accent/50 focus:outline-none focus:ring-1 focus:ring-accent/30"
                      aria-label="Enlace o hash de build compartido"
                    />
                    <div className="flex gap-2">
                      <button
                        onClick={onImport}
                        className="rounded-full bg-accent px-4 py-2 text-xs font-semibold text-accent-foreground transition hover:opacity-90"
                      >
                        cargar
                      </button>
                      <button
                        onClick={() => {
                          setShowImport(false);
                          setImportValue("");
                          setImportStatus("idle");
                        }}
                        className="rounded-full border border-border/60 px-3 py-2 text-xs text-muted-foreground transition hover:text-foreground"
                      >
                        ✕
                      </button>
                    </div>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>

            <svg
              viewBox="0 0 720 620"
              className="relative h-[440px] w-full sm:h-[560px]"
            >
              {/* edges */}
              {placed.map((n) => {
                if (!n.prereq) return null;
                const p = placed.find((x) => x.id === n.prereq);
                if (!p) return null;
                const active = allocated.has(n.id);
                return (
                  <line
                    key={`e-${n.id}`}
                    x1={p.px}
                    y1={p.py}
                    x2={n.px}
                    y2={n.py}
                    stroke={
                      active
                        ? (weapon.accent)
                        : "rgba(180,180,210,0.18)"
                    }
                    strokeWidth={active ? 2 : 1}
                    strokeDasharray={active ? "0" : "4 4"}
                    style={
                      active
                        ? { filter: `drop-shadow(0 0 4px ${weapon.accent})` }
                        : undefined
                    }
                  />
                );
              })}

              {/* center hub */}
              <g>
                <circle
                  cx={360}
                  cy={310}
                  r={28}
                  fill={weapon.accentSoft}
                  stroke={weapon.accent}
                  strokeWidth={1.5}
                />
                <circle
                  cx={360}
                  cy={310}
                  r={14}
                  fill={weapon.accent}
                  opacity={0.6}
                  className="animate-pulse-glow"
                />
                <text
                  x={360}
                  y={316}
                  textAnchor="middle"
                  fontSize={14}
                  fill="#fff"
                  fontWeight={700}
                >
                  ✦
                </text>
                <text
                  x={360}
                  y={356}
                  textAnchor="middle"
                  fontSize={9}
                  fill="rgba(200,200,230,0.7)"
                  className="font-mono"
                >
                  {weapon.name.toUpperCase()}
                </text>
              </g>

              {/* nodes */}
              {placed.map((n) => {
                const r = n.rarity === "legendary" ? 12 : n.rarity === "rare" ? 10 : 8;
                const rs = RARITY_STYLE[n.rarity];
                const isAllocated = allocated.has(n.id);
                const isSel = selected === n.id;
                const can = canAllocate(n);
                const blocked = !isAllocated && !can;
                // Search: a node matches if the query is empty OR its name contains it.
                const q = nodeQuery.trim().toLowerCase();
                const matches = !q || n.name.toLowerCase().includes(q);
                // Dim non-matches when searching; highlight matches with a ring.
                const dimmed = !matches;
                return (
                  <g
                    key={n.id}
                    transform={`translate(${n.px},${n.py})`}
                    onClick={() => toggle(n)}
                    onMouseEnter={() => setHovered(n.id)}
                    onMouseLeave={() => setHovered(null)}
                    onFocus={() => setHovered(n.id)}
                    onBlur={() => setHovered(null)}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === " ") {
                        e.preventDefault();
                        toggle(n);
                      }
                    }}
                    tabIndex={0}
                    role="button"
                    aria-label={`${n.name}, ${RARITY_STYLE[n.rarity].label}, costo ${n.cost} puntos${isAllocated ? ", asignado" : blocked ? ", bloqueado" : ""}`}
                    aria-pressed={isAllocated}
                    className="cursor-pointer outline-none transition-opacity focus-visible:[&>circle]:stroke-white focus-visible:[&>circle]:stroke-[3]"
                    opacity={dimmed ? 0.2 : 1}
                  >
                    {/* search-match highlight ring */}
                    {matches && q && (
                      <circle
                        r={r + 5}
                        fill="none"
                        stroke={weapon.accent}
                        strokeWidth={1.5}
                        opacity={0.8}
                        className="animate-pulse-glow"
                      />
                    )}
                    {/* glow halo */}
                    {(isAllocated || isSel) && (
                      <circle
                        r={r + 8}
                        fill={rs.glow}
                        opacity={0.5}
                        className="animate-pulse-glow"
                      />
                    )}
                    {/* node body */}
                    <circle
                      r={r}
                      fill={isAllocated ? weapon.accent : rs.fill}
                      stroke={isSel ? "#fff" : rs.ring}
                      strokeWidth={isSel ? 2.5 : 1.5}
                      opacity={blocked ? 0.35 : 1}
                    />
                    {/* rarity pip */}
                    {n.rarity !== "common" && (
                      <circle
                        cx={r - 2}
                        cy={-(r - 2)}
                        r={2.5}
                        fill={rs.ring}
                      />
                    )}
                    {/* cost */}
                    <text
                      y={3}
                      textAnchor="middle"
                      fontSize={9}
                      fontWeight={700}
                      fill={isAllocated ? "#0a0a1a" : "#fff"}
                    >
                      {n.cost}
                    </text>
                  </g>
                );
              })}
            </svg>

            {/* Hover tooltip (HTML overlay positioned over the hovered node) */}
            <NodeTooltip
              node={placed.find((p) => p.id === hovered) ?? null}
              accent={weapon.accent}
              accentSoft={weapon.accentSoft}
              allocated={allocated}
              blocked={(n) => !allocated.has(n.id) && !canAllocate(n)}
            />

            {/* legend */}
            <div className="relative z-10 flex flex-wrap items-center justify-center gap-3 border-t border-border/40 px-4 py-2.5 text-[10px] font-mono uppercase tracking-wider text-muted-foreground">
              {(["common", "rare", "legendary"] as const).map((r) => (
                <span key={r} className="flex items-center gap-1.5">
                  <span
                    className="h-2.5 w-2.5 rounded-full"
                    style={{
                      background: RARITY_STYLE[r].ring,
                      boxShadow: `0 0 6px ${RARITY_STYLE[r].glow}`,
                    }}
                  />
                  {RARITY_STYLE[r].label}
                </span>
              ))}
              <span className="text-border">·</span>
              <span>número = costo</span>
            </div>
          </div>

          {/* SIDE: points + selected node */}
          <div className="flex flex-col gap-4">
            {/* budget */}
            <div className="glass-panel rounded-2xl border border-border/60 p-5">
              <div className="flex items-center justify-between">
                <span className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                  Presupuesto (demo Lv {demoLevel})
                </span>
                <span className="font-mono text-[10px] text-muted-foreground">
                  seed {seed}
                </span>
              </div>
              <div className="mt-2 flex items-end justify-between">
                <div>
                  <div className="text-3xl font-bold tabular-nums text-glow-gold">
                    {available}
                  </div>
                  <div className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                    disponibles
                  </div>
                </div>
                <div className="text-right">
                  <div className="text-sm tabular-nums text-muted-foreground">
                    {spent} / {totalPoints}
                  </div>
                  <div className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                    gastados
                  </div>
                </div>
              </div>
              <div className="mt-3 h-2 overflow-hidden rounded-full bg-secondary/60">
                <motion.div
                  className="h-full rounded-full"
                  style={{ background: weapon.accent }}
                  animate={{
                    width: `${(spent / totalPoints) * 100}%`,
                  }}
                  transition={{ duration: 0.4 }}
                />
              </div>
              <button
                onClick={() => setAllocated(new Set())}
                className="mt-3 w-full rounded-full border border-border/60 py-1.5 text-xs text-muted-foreground transition hover:border-destructive/50 hover:text-destructive"
              >
                resetear asignación
              </button>
            </div>

            {/* selected node */}
            <div className="glass-panel min-h-[220px] rounded-2xl border border-border/60 p-5">
              <AnimatePresence mode="wait">
                {sel ? (
                  <motion.div
                    key={sel.id}
                    initial={{ opacity: 0, y: 8 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, y: -8 }}
                    transition={{ duration: 0.2 }}
                  >
                    <div className="flex items-center gap-2">
                      <span
                        className="rounded-full px-2 py-0.5 font-mono text-[9px] uppercase tracking-wider"
                        style={{
                          background: RARITY_STYLE[sel.rarity].fill,
                          color: RARITY_STYLE[sel.rarity].ring,
                        }}
                      >
                        {RARITY_STYLE[sel.rarity].label}
                      </span>
                      <span className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                        {sel.branch}
                      </span>
                    </div>
                    <h4 className="mt-2 text-lg font-semibold text-glow-gold">
                      {sel.name}
                    </h4>
                    <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
                      {sel.effect}
                    </p>
                    <div className="mt-4 flex items-center justify-between text-xs">
                      <span className="text-muted-foreground">
                        Costo:{" "}
                        <span className="font-semibold text-foreground">
                          {sel.cost} pts
                        </span>
                      </span>
                      {sel.prereq && (
                        <span className="text-muted-foreground">
                          req:{" "}
                          <span
                            className={
                              allocated.has(sel.prereq)
                                ? "text-primary"
                                : "text-destructive"
                            }
                          >
                            {
                              placed.find((p) => p.id === sel.prereq)?.name
                            }
                          </span>
                        </span>
                      )}
                    </div>
                    <button
                      onClick={() => toggle(sel)}
                      disabled={!canAllocate(sel) && !allocated.has(sel.id)}
                      className="mt-4 w-full rounded-full py-2 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-40"
                      style={{
                        background: allocated.has(sel.id)
                          ? "transparent"
                          : weapon.accent,
                        color: allocated.has(sel.id)
                          ? "var(--muted-foreground)"
                          : "#0a0a1a",
                        border: allocated.has(sel.id)
                          ? "1px solid var(--border)"
                          : "none",
                      }}
                    >
                      {allocated.has(sel.id)
                        ? "✓ asignado — clic para quitar"
                        : canAllocate(sel)
                          ? "asignar nodo"
                          : "requisitos insuficientes"}
                    </button>
                  </motion.div>
                ) : (
                  <div className="flex h-full min-h-[180px] flex-col items-center justify-center text-center">
                    <span className="text-3xl opacity-50">✦</span>
                    <p className="mt-3 text-sm text-muted-foreground">
                      Selecciona un nodo en la constelación para ver su efecto
                      y asignarlo.
                    </p>
                  </div>
                )}
              </AnimatePresence>
            </div>
          </div>

          {/* Build summary — full width */}
          <BuildSummary
            weapon={weapon}
            allocated={allocated}
            placed={placed}
          />

          {/* Build comparison — snapshot vs current */}
          <BuildCompare
            weapon={weapon}
            currentAllocated={allocated}
            placed={placed}
            snapshotA={snapshotA}
            onSaveSnapshot={() => {
              const ids = weapon.skillTree.map((n) => n.id);
              const enc = encodeBuild(weaponId, seed, [...allocated], ids);
              setSnapshotA(enc);
              saveString(SNAPSHOT_KEY, enc);
            }}
            onClearSnapshot={() => {
              setSnapshotA(null);
              removeKey(SNAPSHOT_KEY);
            }}
          />
        </div>
      </div>
    </section>
  );
}

function BuildSummary({
  weapon,
  allocated,
  placed,
}: {
  weapon: (typeof WEAPONS)[number];
  allocated: Set<string>;
  placed: PlacedNode[];
}) {
  const allocatedNodes = placed.filter((n) => allocated.has(n.id));
  const byBranch = new Map<string, PlacedNode[]>();
  for (const n of allocatedNodes) {
    const arr = byBranch.get(n.branch) ?? [];
    arr.push(n);
    byBranch.set(n.branch, arr);
  }
  const totalCost = allocatedNodes.reduce((s, n) => s + n.cost, 0);
  const rarityCount = {
    common: allocatedNodes.filter((n) => n.rarity === "common").length,
    rare: allocatedNodes.filter((n) => n.rarity === "rare").length,
    legendary: allocatedNodes.filter((n) => n.rarity === "legendary").length,
  };

  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ duration: 0.4 }}
      className="glass-panel mt-6 rounded-3xl border border-border/60 p-6"
    >
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <span
            className="flex h-10 w-10 items-center justify-center rounded-xl text-lg"
            style={{
              background: weapon.accentSoft,
              color: weapon.accent,
            }}
          >
            ✦
          </span>
          <div>
            <h3 className="text-base font-semibold">
              Resumen de build — {weapon.name}
            </h3>
            <p className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
              {allocatedNodes.length} nodos · {totalCost} pts invertidos
            </p>
          </div>
        </div>
        <div className="flex items-center gap-3 text-xs">
          <RarityPip label="Común" count={rarityCount.common} color="rgba(200,200,230,0.9)" />
          <RarityPip label="Raro" count={rarityCount.rare} color="rgba(140,180,255,0.95)" />
          <RarityPip
            label="Legendario"
            count={rarityCount.legendary}
            color="rgba(255,220,140,1)"
          />
        </div>
      </div>

      {allocatedNodes.length === 0 ? (
        <p className="mt-4 rounded-xl border border-dashed border-border/50 p-4 text-center text-sm text-muted-foreground">
          Ningún nodo asignado aún. Haz clic en estrellas de la constelación
          para construir tu build.
        </p>
      ) : (
        <div className="mt-4 grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {[...byBranch.entries()].map(([branch, nodes]) => {
            const branchMeta = weapon.branches.find((b) => b.name === branch);
            return (
              <div
                key={branch}
                className="rounded-2xl border border-border/50 bg-card/30 p-3"
              >
                <div className="flex items-center gap-2">
                  <span
                    className="flex h-6 w-6 items-center justify-center rounded-md text-xs"
                    style={{
                      background: weapon.accentSoft,
                      color: weapon.accent,
                    }}
                  >
                    {branchMeta?.icon ?? "•"}
                  </span>
                  <span className="text-xs font-semibold">{branch}</span>
                  <span className="ml-auto font-mono text-[10px] text-muted-foreground">
                    {nodes.length}
                  </span>
                </div>
                <ul className="mt-2 space-y-1">
                  {nodes.map((n) => (
                    <li
                      key={n.id}
                      className="flex items-start gap-1.5 text-[11px] leading-snug"
                    >
                      <span
                        className="mt-1 h-1 w-1 shrink-0 rounded-full"
                        style={{
                          background:
                            n.rarity === "legendary"
                              ? "rgba(255,220,140,1)"
                              : n.rarity === "rare"
                                ? "rgba(140,180,255,0.95)"
                                : "rgba(200,200,230,0.7)",
                        }}
                      />
                      <span className="text-muted-foreground">
                        <span className="text-foreground">{n.name}</span>
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            );
          })}
        </div>
      )}
    </motion.div>
  );
}

function RarityPip({
  label,
  count,
  color,
}: {
  label: string;
  count: number;
  color: string;
}) {
  return (
    <span className="flex items-center gap-1.5">
      <span
        className="h-2 w-2 rounded-full"
        style={{
          background: color,
          boxShadow: count > 0 ? `0 0 6px ${color}` : "none",
          opacity: count > 0 ? 1 : 0.3,
        }}
      />
      <span className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
        {label}
      </span>
      <span className="tabular-nums text-foreground">{count}</span>
    </span>
  );
}

/**
 * Floating HTML tooltip that appears over the hovered skill-tree node.
 * Positioned using the node's normalized SVG coords (px/720, py/620) as
 * percentages, so it tracks the SVG box regardless of render size.
 */
function NodeTooltip({
  node,
  accent,
  accentSoft,
  allocated,
  blocked,
}: {
  node: PlacedNode | null;
  accent: string;
  accentSoft: string;
  allocated: Set<string>;
  blocked: (n: PlacedNode) => boolean;
}) {
  if (!node) return null;
  const leftPct = (node.px / 720) * 100;
  const topPct = (node.py / 620) * 100;
  const isAllocated = allocated.has(node.id);
  const isBlocked = blocked(node);
  const rs = RARITY_STYLE[node.rarity];
  // Flip the tooltip to the left/right of the node depending on which side
  // of the canvas it's on, so it doesn't overflow the container.
  const isRight = leftPct > 50;
  const isBottom = topPct > 50;

  return (
    <AnimatePresence>
      <motion.div
        initial={{ opacity: 0, scale: 0.9 }}
        animate={{ opacity: 1, scale: 1 }}
        exit={{ opacity: 0, scale: 0.9 }}
        transition={{ duration: 0.15 }}
        className="pointer-events-none absolute z-30 w-56"
        style={{
          left: `${leftPct}%`,
          top: `${topPct}%`,
          transform: `translate(${isRight ? "-110%" : "10%"}, ${isBottom ? "-110%" : "10%"})`,
        }}
      >
        <div
          className="rounded-xl border p-3 shadow-2xl backdrop-blur-md"
          style={{
            borderColor: `${accent}66`,
            background: "oklch(0.16 0.035 285 / 92%)",
            boxShadow: `0 8px 32px -8px ${accent}44, 0 0 0 1px ${accent}33`,
          }}
        >
          <div className="flex items-center gap-2">
            <span
              className="rounded-full px-1.5 py-0.5 font-mono text-[8px] uppercase tracking-wider"
              style={{ background: accentSoft, color: accent }}
            >
              {rs.label}
            </span>
            {isAllocated && (
              <span className="rounded-full bg-primary/20 px-1.5 py-0.5 font-mono text-[8px] uppercase tracking-wider text-primary">
                ✓ asignado
              </span>
            )}
            {isBlocked && !isAllocated && (
              <span className="rounded-full bg-destructive/20 px-1.5 py-0.5 font-mono text-[8px] uppercase tracking-wider text-destructive">
                bloqueado
              </span>
            )}
          </div>
          <h4 className="mt-1.5 text-sm font-semibold text-foreground">
            {node.name}
          </h4>
          <p className="mt-1 text-xs leading-snug text-muted-foreground">
            {node.effect}
          </p>
          <div className="mt-2 flex items-center justify-between border-t border-border/40 pt-1.5 font-mono text-[10px] text-muted-foreground">
            <span>
              costo{" "}
              <span className="font-semibold text-primary">{node.cost} pts</span>
            </span>
            {node.prereq && (
              <span
                className={
                  allocated.has(node.prereq) ? "text-primary" : "text-destructive"
                }
              >
                req: {node.prereq.replace(/-\d+$/, "")}
              </span>
            )}
          </div>
        </div>
      </motion.div>
    </AnimatePresence>
  );
}

/**
 * Build comparison panel: lets users snapshot the current build ("build A"),
 * then compare it against the live build. Highlights nodes that are:
 *  - added (in current, not in A) — green
 *  - removed (in A, not in current) — red
 *  - unchanged — dim
 * Only compares when the snapshot's weapon matches the current weapon.
 */
function BuildCompare({
  weapon,
  currentAllocated,
  placed,
  snapshotA,
  onSaveSnapshot,
  onClearSnapshot,
}: {
  weapon: (typeof WEAPONS)[number];
  currentAllocated: Set<string>;
  placed: PlacedNode[];
  snapshotA: string | null;
  onSaveSnapshot: () => void;
  onClearSnapshot: () => void;
}) {
  const allNodeIds = weapon.skillTree.map((n) => n.id);

  const snapshotAllocated = useMemo(() => {
    if (!snapshotA) return null;
    const shared = decodeBuild(snapshotA, allNodeIds);
    if (!shared || shared.weaponId !== weapon.id) return null;
    const ids = new Set<string>();
    for (const idx of shared.nodeIndices) {
      const id = weapon.skillTree[idx]?.id;
      if (id) ids.add(id);
    }
    return ids;
  }, [snapshotA, allNodeIds, weapon]);

  const diff = useMemo(() => {
    if (!snapshotAllocated) return null;
    const added: string[] = [];
    const removed: string[] = [];
    const unchanged: string[] = [];
    for (const n of placed) {
      const inCur = currentAllocated.has(n.id);
      const inSnap = snapshotAllocated.has(n.id);
      if (inCur && !inSnap) added.push(n.id);
      else if (!inCur && inSnap) removed.push(n.id);
      else if (inCur && inSnap) unchanged.push(n.id);
    }
    return { added, removed, unchanged: unchanged.length };
  }, [snapshotAllocated, currentAllocated, placed]);

  const snapshotMeta = useMemo(() => {
    if (!snapshotA) return null;
    const shared = decodeBuild(snapshotA, allNodeIds);
    if (!shared) return null;
    const snapWeapon = WEAPONS.find((w) => w.id === shared.weaponId);
    return snapWeapon
      ? { name: snapWeapon.name, count: shared.nodeIndices.length }
      : null;
  }, [snapshotA, allNodeIds]);

  const sameWeapon = snapshotAllocated !== null;

  return (
    <motion.div
      initial={{ opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ duration: 0.4 }}
      className="glass-panel mt-6 rounded-3xl border border-border/60 p-6"
    >
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <span
            className="flex h-10 w-10 items-center justify-center rounded-xl text-lg"
            style={{
              background: weapon.accentSoft,
              color: weapon.accent,
            }}
          >
            ⚖
          </span>
          <div>
            <h3 className="text-base font-semibold">Comparar builds</h3>
            <p className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
              Guarda una snapshot y compara contra la build actual
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {!snapshotA ? (
            <button
              onClick={onSaveSnapshot}
              className="rounded-full border border-accent/40 bg-accent/10 px-4 py-1.5 text-xs font-semibold text-accent transition hover:border-accent hover:bg-accent/20"
            >
              📸 guardar build A
            </button>
          ) : (
            <>
              <span className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                A: {snapshotMeta?.name} · {snapshotMeta?.count} nodos
              </span>
              <button
                onClick={onSaveSnapshot}
                disabled={!sameWeapon}
                className="rounded-full border border-border/60 px-3 py-1.5 text-xs text-muted-foreground transition hover:border-accent/50 hover:text-accent disabled:opacity-40"
                title={
                  sameWeapon
                    ? "Sobrescribir snapshot A"
                    : "Cambia al arma de la snapshot para sobrescribir"
                }
              >
                ↻ actualizar
              </button>
              <button
                onClick={onClearSnapshot}
                className="rounded-full border border-destructive/40 px-3 py-1.5 text-xs text-destructive transition hover:bg-destructive/10"
              >
                ✕ borrar
              </button>
            </>
          )}
        </div>
      </div>

      {snapshotA && !sameWeapon && (
        <div className="mt-4 rounded-xl border border-destructive/30 bg-destructive/5 p-3 text-xs text-muted-foreground">
          ⚠ La snapshot A es para{" "}
          <span className="text-foreground">
            {(() => {
              const shared = decodeBuild(snapshotA, allNodeIds);
              return shared ? WEAPONS.find((w) => w.id === shared.weaponId)?.name : "?";
            })()}
          </span>
          . Cambia a esa arma para comparar, o borra la snapshot.
        </div>
      )}

      {diff && sameWeapon && (
        <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-3">
          <DiffStat
            label="Añadidos"
            count={diff.added.length}
            color="#7ee3c4"
            icon="+"
          />
          <DiffStat
            label="Quitados"
            count={diff.removed.length}
            color="#ff7a7a"
            icon="−"
          />
          <DiffStat
            label="Sin cambios"
            count={diff.unchanged}
            color="rgba(200,200,230,0.7)"
            icon="="
          />
        </div>
      )}

      {diff && sameWeapon && (diff.added.length > 0 || diff.removed.length > 0) && (
        <div className="mt-4 space-y-2">
          {diff.added.length > 0 && (
            <DiffList
              title="Nodos añadidos (en actual, no en A)"
              ids={diff.added}
              placed={placed}
              color="#7ee3c4"
              sign="+"
            />
          )}
          {diff.removed.length > 0 && (
            <DiffList
              title="Nodos quitados (en A, no en actual)"
              ids={diff.removed}
              placed={placed}
              color="#ff7a7a"
              sign="−"
            />
          )}
        </div>
      )}
      {diff && sameWeapon && diff.added.length === 0 && diff.removed.length === 0 && (
        <p className="mt-4 rounded-xl border border-border/50 bg-card/30 p-3 text-center text-xs text-muted-foreground">
          ✓ Las builds son idénticas
        </p>
      )}
    </motion.div>
  );
}

function DiffStat({
  label,
  count,
  color,
  icon,
}: {
  label: string;
  count: number;
  color: string;
  icon: string;
}) {
  return (
    <div className="rounded-2xl border border-border/50 bg-card/30 p-3 text-center">
      <div
        className="text-2xl font-bold tabular-nums"
        style={{ color, textShadow: count > 0 ? `0 0 12px ${color}66` : "none" }}
      >
        {icon}
        {count}
      </div>
      <div className="mt-0.5 font-mono text-[9px] uppercase tracking-wider text-muted-foreground">
        {label}
      </div>
    </div>
  );
}

function DiffList({
  title,
  ids,
  placed,
  color,
  sign,
}: {
  title: string;
  ids: string[];
  placed: PlacedNode[];
  color: string;
  sign: string;
}) {
  const nodes = ids
    .map((id) => placed.find((p) => p.id === id))
    .filter((n): n is PlacedNode => Boolean(n));
  return (
    <div className="rounded-2xl border border-border/40 bg-card/20 p-3">
      <div className="font-mono text-[9px] uppercase tracking-wider text-muted-foreground">
        {title}
      </div>
      <div className="mt-2 flex flex-wrap gap-1.5">
        {nodes.map((n) => (
          <span
            key={n.id}
            className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs"
            style={{ background: `${color}1a`, color, border: `1px solid ${color}44` }}
          >
            <span className="font-bold">{sign}</span>
            {n.name}
          </span>
        ))}
      </div>
    </div>
  );
}
