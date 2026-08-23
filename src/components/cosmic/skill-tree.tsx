"use client";

import { useEffect, useMemo, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import {
  cumulativeSkillPoints,
  WEAPONS,
  type SkillNode,
  type WeaponId,
} from "@/lib/mod-data";
import {
  copyShareUrl,
  decodeBuild,
  encodeBuild,
  writeHashBuild,
} from "@/lib/build-share";
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
    setHydrated(true);
    /* eslint-enable react-hooks/set-state-in-effect */
  }, []);

  // Persist current build to URL hash — but only AFTER hydration so we don't
  // overwrite the shared hash we just restored with empty defaults.
  useEffect(() => {
    if (!hydrated) return;
    const enc = encodeBuild(weaponId, seed, [...allocated], allNodeIds);
    writeHashBuild(enc);
  }, [allocated, weaponId, seed, allNodeIds, hydrated]);

  const [shareStatus, setShareStatus] = useState<
    "idle" | "copied" | "error"
  >("idle");
  const onShare = async () => {
    const enc = encodeBuild(weaponId, seed, [...allocated], allNodeIds);
    writeHashBuild(enc);
    const ok = await copyShareUrl(enc);
    setShareStatus(ok ? "copied" : "error");
    window.setTimeout(() => setShareStatus("idle"), 2200);
  };

  // Build import: paste a share URL or hash to load someone else's build.
  const [showImport, setShowImport] = useState(false);
  const [importValue, setImportValue] = useState("");
  const [importStatus, setImportStatus] = useState<
    "idle" | "ok" | "error"
  >("idle");
  const onImport = () => {
    // Accept either a full URL (...#v1.x.y.z) or a bare hash (v1.x.y.z / #v1.x.y.z).
    const raw = importValue.trim();
    if (!raw) return;
    const hashIdx = raw.indexOf("#v1.");
    const hash =
      hashIdx >= 0 ? raw.slice(hashIdx) : raw.startsWith("v1.") ? `#${raw}` : "";
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
                return (
                  <g
                    key={n.id}
                    transform={`translate(${n.px},${n.py})`}
                    onClick={() => toggle(n)}
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
                    className="cursor-pointer outline-none focus-visible:[&>circle]:stroke-white focus-visible:[&>circle]:stroke-[3]"
                  >
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
