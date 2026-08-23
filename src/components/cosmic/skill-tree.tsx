"use client";

import { useMemo, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import {
  cumulativeSkillPoints,
  WEAPONS,
  type SkillNode,
  type WeaponId,
} from "@/lib/mod-data";
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

export function SkillTreeView() {
  const [weaponId, setWeaponId] = useState<WeaponId>("book");
  const [seed, setSeed] = useState(1337);
  const [allocated, setAllocated] = useState<Set<string>>(new Set());
  const [selected, setSelected] = useState<string | null>(null);

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

        <div className="mt-8 grid gap-6 lg:grid-cols-[1.4fr_0.6fr]">
          {/* CANVAS */}
          <div className="relative overflow-hidden rounded-3xl border border-border/60 bg-card/30 p-2">
            <div className="pointer-events-none absolute inset-0 bg-cosmic-grid opacity-25" />
            <div className="pointer-events-none absolute inset-0">
              <div className="absolute left-1/2 top-1/2 h-40 w-40 -translate-x-1/2 -translate-y-1/2 rounded-full bg-primary/10 blur-3xl" />
            </div>

            {/* controls */}
            <div className="absolute right-3 top-3 z-10 flex gap-2">
              <button
                onClick={onReroll}
                className="rounded-full border border-border/60 bg-card/80 px-3 py-1.5 text-xs text-muted-foreground backdrop-blur transition hover:border-primary/50 hover:text-primary"
              >
                ↻ re-generar (seed {seed})
              </button>
            </div>

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
                    className="cursor-pointer"
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
        </div>
      </div>
    </section>
  );
}
