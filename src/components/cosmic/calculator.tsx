"use client";

import { useMemo, useState } from "react";
import { motion } from "framer-motion";
import {
  cumulativeSkillPoints,
  cumulativeXp,
  MILESTONES,
  pointsForLevel,
  SKILL_TIERS,
  xpForNextLevel,
} from "@/lib/mod-data";
import { SectionHeading } from "./lore-section";
import { cn } from "@/lib/utils";

const PRESETS = [1, 10, 25, 50, 75, 100, 150, 200, 500];

export function ProgressionCalculator() {
  const [level, setLevel] = useState(50);

  const data = useMemo(() => {
    const totalPoints = cumulativeSkillPoints(level);
    const atLevel = pointsForLevel(level);
    const xpNext = xpForNextLevel(level);
    const totalXp = cumulativeXp(level);
    const nextMilestone =
      MILESTONES.find((m) => m.level > level) ?? MILESTONES[MILESTONES.length - 1];
    const prevMilestone = [...MILESTONES].reverse().find((m) => m.level <= level);
    return {
      totalPoints,
      atLevel,
      xpNext,
      totalXp,
      nextMilestone,
      prevMilestone,
    };
  }, [level]);

  const fmt = (n: number) =>
    n >= 1_000_000 ? `${(n / 1_000_000).toFixed(2)}M` : n.toLocaleString("en-US");

  return (
    <section id="progression" className="relative py-24 sm:py-32">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <SectionHeading
          kicker="Progresión"
          title={
            <>
              Sube <span className="text-glow-gold">infinitamente</span>, gana
              más cada nivel
            </>
          }
          subtitle="Cada enemigo que matas otorga XP. Los puntos de habilidad se aceleran por tramos de 10 niveles — hasta +10 por nivel post-100, para siempre."
        />

        <div className="mt-14 grid gap-6 lg:grid-cols-[1fr_0.9fr]">
          {/* LEFT: slider + readout */}
          <div className="glass-panel rounded-3xl border border-border/60 p-6 sm:p-8">
            <div className="flex items-end justify-between">
              <div>
                <div className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                  Nivel del arma
                </div>
                <div className="mt-1 flex items-baseline gap-2">
                  <span className="text-6xl font-bold tabular-nums text-glow-gold">
                    {level}
                  </span>
                  <span className="font-mono text-sm text-muted-foreground">
                    / ∞
                  </span>
                </div>
              </div>
              <div className="text-right">
                <div className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                  Puntos / nivel
                </div>
                <div className="mt-1 text-3xl font-bold tabular-nums text-primary">
                  +{data.atLevel}
                </div>
              </div>
            </div>

            {/* slider */}
            <div className="mt-6">
              <input
                type="range"
                min={1}
                max={250}
                value={level}
                onChange={(e) => setLevel(Number(e.target.value))}
                className="cosmic-range w-full"
                aria-label="Nivel del arma"
              />
              <div className="mt-2 flex justify-between font-mono text-[10px] text-muted-foreground">
                <span>1</span>
                <span>50</span>
                <span>100</span>
                <span>150</span>
                <span>200</span>
                <span>250+</span>
              </div>
            </div>

            {/* presets */}
            <div className="mt-4 flex flex-wrap gap-1.5">
              {PRESETS.map((p) => (
                <button
                  key={p}
                  onClick={() => setLevel(p)}
                  className={cn(
                    "rounded-full border px-3 py-1 text-xs transition",
                    level === p
                      ? "border-primary bg-primary/15 text-primary"
                      : "border-border/60 text-muted-foreground hover:border-primary/40 hover:text-foreground",
                  )}
                >
                  Lv {p}
                </button>
              ))}
            </div>

            {/* big readouts */}
            <div className="mt-8 grid grid-cols-2 gap-3">
              <Readout
                label="Puntos acumulados"
                value={fmt(data.totalPoints)}
                accent="primary"
              />
              <Readout
                label="XP para siguiente nivel"
                value={fmt(data.xpNext)}
                accent="accent"
              />
              <Readout
                label="XP total acumulada"
                value={fmt(data.totalXp)}
                accent="teal"
              />
              <Readout
                label="Tramo actual"
                value={`+${data.atLevel}/lv`}
                accent="primary"
              />
            </div>

            {/* progress to next milestone */}
            <div className="mt-6 rounded-2xl border border-border/50 bg-card/30 p-4">
              <div className="flex items-center justify-between text-xs">
                <span className="text-muted-foreground">
                  Hito siguiente:{" "}
                  <span className="text-foreground">
                    {data.nextMilestone.label}
                  </span>
                </span>
                <span className="font-mono text-muted-foreground">
                  Lv {data.nextMilestone.level}
                </span>
              </div>
              <div className="mt-2 h-2 overflow-hidden rounded-full bg-secondary/60">
                <motion.div
                  className="h-full rounded-full bg-gradient-to-r from-primary via-amber-300 to-accent"
                  initial={{ width: 0 }}
                  animate={{
                    width: `${Math.min(
                      100,
                      (level / data.nextMilestone.level) * 100,
                    )}%`,
                  }}
                  transition={{ duration: 0.6, ease: "easeOut" }}
                />
              </div>
              {data.prevMilestone && level >= data.prevMilestone.level && (
                <p className="mt-2 text-[11px] text-muted-foreground">
                  ✓ Desbloqueado a Lv {data.prevMilestone.level}:{" "}
                  <span className="text-foreground">
                    {data.prevMilestone.label}
                  </span>{" "}
                  — {data.prevMilestone.desc}
                </p>
              )}
            </div>
          </div>

          {/* RIGHT: tier table + milestones */}
          <div className="flex flex-col gap-6">
            <div className="glass-panel rounded-3xl border border-border/60 p-6">
              <div className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                Tabla de puntos por tramo
              </div>
              <ul className="mt-3 max-h-72 space-y-1 overflow-y-auto cosmic-scroll pr-1">
                {SKILL_TIERS.map((t) => {
                  const isCurrent =
                    level >= t.min && level <= (t.max === Infinity ? 9999 : t.max);
                  const cumulative = cumulativeSkillPoints(
                    t.max === Infinity ? level : Math.min(level, t.max),
                  );
                  return (
                    <li
                      key={`${t.min}-${t.max}`}
                      className={cn(
                        "flex items-center justify-between rounded-lg px-3 py-2 text-sm transition",
                        isCurrent
                          ? "bg-primary/10 text-primary"
                          : "text-muted-foreground hover:bg-secondary/40",
                      )}
                    >
                      <span className="font-mono text-xs">
                        {t.min}–{t.max === Infinity ? "∞" : t.max}
                      </span>
                      <span className="font-mono">+{t.perLevel}/lv</span>
                      <span className="tabular-nums">
                        {cumulative.toLocaleString("en-US")} pts
                      </span>
                    </li>
                  );
                })}
              </ul>
            </div>

            <div className="glass-panel rounded-3xl border border-border/60 p-6">
              <div className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                Hitos cósmicos
              </div>
              <ul className="mt-3 space-y-2.5">
                {MILESTONES.map((m) => {
                  const unlocked = level >= m.level;
                  return (
                    <li
                      key={m.level}
                      className={cn(
                        "flex items-start gap-3 rounded-lg border p-3 transition",
                        unlocked
                          ? "border-primary/40 bg-primary/5"
                          : "border-border/40 opacity-70",
                      )}
                    >
                      <span
                        className={cn(
                          "mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full text-xs font-bold",
                          unlocked
                            ? "bg-primary text-primary-foreground"
                            : "bg-secondary text-muted-foreground",
                        )}
                      >
                        {unlocked ? "✓" : m.level}
                      </span>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2">
                          <span className="text-sm font-semibold">
                            {m.label}
                          </span>
                          <span className="font-mono text-[10px] text-muted-foreground">
                            Lv {m.level}
                          </span>
                        </div>
                        <p className="text-xs text-muted-foreground">
                          {m.desc}
                        </p>
                      </div>
                    </li>
                  );
                })}
              </ul>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function Readout({
  label,
  value,
  accent,
}: {
  label: string;
  value: string;
  accent: "primary" | "accent" | "teal";
}) {
  const glow =
    accent === "primary"
      ? "text-glow-gold"
      : accent === "accent"
        ? "text-glow-violet"
        : "text-glow-teal";
  return (
    <div className="rounded-xl border border-border/50 bg-card/40 p-4">
      <div className="font-mono text-[9px] uppercase tracking-wider text-muted-foreground">
        {label}
      </div>
      <div className={cn("mt-1 text-2xl font-bold tabular-nums", glow)}>
        {value}
      </div>
    </div>
  );
}
