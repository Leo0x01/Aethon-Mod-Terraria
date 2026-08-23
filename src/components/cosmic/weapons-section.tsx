"use client";

import { useState } from "react";
import Image from "next/image";
import { AnimatePresence, motion } from "framer-motion";
import { WEAPONS, type WeaponId } from "@/lib/mod-data";
import { SectionHeading } from "./lore-section";
import { cn } from "@/lib/utils";

export function WeaponsSection() {
  const [active, setActive] = useState<WeaponId | null>(null);
  const weapon = active ? WEAPONS.find((w) => w.id === active)! : null;

  return (
    <section id="weapons" className="relative py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <SectionHeading
          kicker="Las Cuatro Formas"
          title={
            <>
              Una luz, <span className="text-glow-gold">cuatro destinos</span>
            </>
          }
          subtitle="El Genesis Shard se presenta como un m mote brillante. Al bondéate a él, eliges su forma — permanente para ese personaje. Cada forma abre una progresión completamente distinta."
        />

        <div className="mt-14 grid gap-8 lg:grid-cols-[0.9fr_1.1fr]">
          {/* LEFT: the light / weapon display */}
          <div className="relative flex min-h-[420px] items-center justify-center rounded-3xl border border-border/60 bg-card/30 p-6 sm:min-h-[520px]">
            <div className="pointer-events-none absolute inset-0 overflow-hidden rounded-3xl">
              <div className="absolute inset-0 bg-cosmic-grid opacity-30" />
            </div>

            <AnimatePresence mode="wait">
              {!weapon ? (
                <motion.div
                  key="light"
                  initial={{ opacity: 0, scale: 0.8 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0, scale: 0.6 }}
                  transition={{ duration: 0.5 }}
                  className="relative z-10 flex flex-col items-center text-center"
                >
                  <div className="relative flex h-44 w-44 items-center justify-center sm:h-56 sm:w-56">
                    <div className="absolute inset-0 animate-pulse-glow rounded-full bg-primary/40 blur-2xl" />
                    <div className="absolute inset-6 animate-pulse-glow rounded-full bg-accent/30 blur-xl" />
                    <div className="absolute inset-10 rounded-full border border-primary/30" />
                    <div className="absolute inset-16 rounded-full border border-accent/30" />
                    <motion.div
                      animate={{ rotate: 360 }}
                      transition={{ duration: 20, repeat: Infinity, ease: "linear" }}
                      className="absolute inset-0"
                    >
                      <span className="absolute left-1/2 top-0 h-2 w-2 -translate-x-1/2 rounded-full bg-white shadow-[0_0_12px_white]" />
                      <span className="absolute bottom-2 right-6 h-1.5 w-1.5 rounded-full bg-primary shadow-[0_0_10px_currentColor]" />
                    </motion.div>
                    <span className="relative text-5xl">✦</span>
                  </div>
                  <p className="mt-6 font-mono text-xs uppercase tracking-[0.25em] text-muted-foreground">
                    Genesis Shard
                  </p>
                  <p className="mt-1 max-w-xs text-sm text-muted-foreground">
                    Elige una forma para bondéate al fragmento
                  </p>
                </motion.div>
              ) : (
                <motion.div
                  key={weapon.id}
                  initial={{ opacity: 0, scale: 0.7, rotate: -8 }}
                  animate={{ opacity: 1, scale: 1, rotate: 0 }}
                  exit={{ opacity: 0, scale: 0.7 }}
                  transition={{ duration: 0.55, ease: [0.22, 1, 0.36, 1] }}
                  className="relative z-10 flex flex-col items-center text-center"
                >
                  <div
                    className="relative flex h-44 w-44 items-center justify-center sm:h-64 sm:w-64"
                    style={{
                      boxShadow: `0 0 60px -10px ${weapon.accent}66`,
                    }}
                  >
                    <div
                      className="absolute inset-0 animate-pulse-glow rounded-full blur-2xl"
                      style={{ background: weapon.accentSoft }}
                    />
                    <Image
                      src={weapon.image}
                      alt={weapon.name}
                      width={280}
                      height={280}
                      className="relative animate-float-slow object-contain"
                    />
                  </div>
                  <p
                    className="mt-4 font-mono text-xs uppercase tracking-[0.25em]"
                    style={{ color: weapon.accent }}
                  >
                    {weapon.classLabel}
                  </p>
                  <h3 className="mt-1 text-2xl font-bold text-glow-gold sm:text-3xl">
                    {weapon.name}
                  </h3>
                  <p className="text-sm italic text-muted-foreground">
                    {weapon.epithet}
                  </p>
                  <p className="mt-2 max-w-xs text-sm text-muted-foreground">
                    {weapon.fantasy}
                  </p>
                </motion.div>
              )}
            </AnimatePresence>

            {weapon && (
              <button
                onClick={() => setActive(null)}
                className="absolute right-4 top-4 z-20 rounded-full border border-border/60 bg-card/70 px-3 py-1 text-xs text-muted-foreground transition hover:border-primary/50 hover:text-primary"
              >
                ↺ revertir
              </button>
            )}
          </div>

          {/* RIGHT: selector + details */}
          <div className="flex flex-col gap-4">
            {/* selector */}
            <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
              {WEAPONS.map((w) => (
                <button
                  key={w.id}
                  onClick={() => setActive(w.id)}
                  className={cn(
                    "group relative overflow-hidden rounded-xl border p-3 text-left transition",
                    active === w.id
                      ? "border-transparent"
                      : "border-border/60 bg-card/40 hover:border-primary/40",
                  )}
                  style={
                    active === w.id
                      ? {
                          borderColor: w.accent,
                          boxShadow: `0 0 24px -6px ${w.accent}66`,
                        }
                      : undefined
                  }
                >
                  <div
                    className="mb-2 h-1 w-8 rounded-full transition group-hover:w-12"
                    style={{ background: w.accent }}
                  />
                  <div className="text-sm font-semibold">{w.name}</div>
                  <div className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                    {w.classLabel}
                  </div>
                </button>
              ))}
            </div>

            {/* details */}
            <AnimatePresence mode="wait">
              {!weapon ? (
                <motion.div
                  key="empty"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="flex flex-1 flex-col justify-center rounded-2xl border border-dashed border-border/60 p-6 text-center"
                >
                  <p className="text-sm text-muted-foreground">
                    Selecciona una forma de arma arriba para ver sus
                    estadísticas, ramas de habilidades y descripción.
                  </p>
                </motion.div>
              ) : (
                <motion.div
                  key={weapon.id}
                  initial={{ opacity: 0, y: 12 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -12 }}
                  transition={{ duration: 0.35 }}
                  className="flex flex-1 flex-col gap-4"
                >
                  {/* stats */}
                  <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                    {weapon.stats.map((s) => (
                      <div
                        key={s.label}
                        className="rounded-xl border border-border/50 bg-card/40 p-3"
                      >
                        <div className="font-mono text-[9px] uppercase tracking-wider text-muted-foreground">
                          {s.label}
                        </div>
                        <div
                          className="mt-1 text-sm font-semibold"
                          style={{ color: weapon.accent }}
                        >
                          {s.value}
                        </div>
                      </div>
                    ))}
                  </div>

                  {/* base projectiles */}
                  <div className="rounded-2xl border border-border/50 bg-card/30 p-4">
                    <div className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                      Proyectiles base
                    </div>
                    <div className="mt-2 flex flex-wrap gap-2">
                      {weapon.baseProjectiles.map((p) => (
                        <span
                          key={p}
                          className="rounded-full border border-border/60 bg-secondary/40 px-3 py-1 text-xs"
                          style={{ color: weapon.accent }}
                        >
                          {p}
                        </span>
                      ))}
                    </div>
                  </div>

                  {/* branches */}
                  <div className="rounded-2xl border border-border/50 bg-card/30 p-4">
                    <div className="flex items-center justify-between">
                      <div className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                        Ramas del árbol
                      </div>
                      <span className="font-mono text-[10px] text-muted-foreground">
                        6 ramas
                      </span>
                    </div>
                    <ul className="mt-3 grid gap-2 sm:grid-cols-2">
                      {weapon.branches.map((b) => (
                        <li
                          key={b.id}
                          className="flex items-start gap-2.5 rounded-lg border border-border/40 bg-card/40 p-2.5"
                        >
                          <span
                            className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-md text-sm"
                            style={{
                              background: weapon.accentSoft,
                              color: weapon.accent,
                            }}
                          >
                            {b.icon}
                          </span>
                          <div className="min-w-0">
                            <div className="flex items-center gap-1.5">
                              <span className="text-sm font-medium">
                                {b.name}
                              </span>
                              {b.capstone && (
                                <span className="rounded bg-accent/20 px-1 font-mono text-[8px] uppercase tracking-wider text-accent">
                                  capstone
                                </span>
                              )}
                            </div>
                            <p className="text-xs text-muted-foreground">
                              {b.description}
                            </p>
                          </div>
                        </li>
                      ))}
                    </ul>
                  </div>

                  {/* lore absorption callout */}
                  <div
                    className="rounded-2xl border p-4"
                    style={{
                      borderColor: `${weapon.accent}55`,
                      background: weapon.accentSoft,
                    }}
                  >
                    <div className="flex items-center gap-2">
                      <span
                        className="flex h-6 w-6 items-center justify-center rounded-full text-xs"
                        style={{ background: weapon.accent, color: "#0a0a1a" }}
                      >
                        ⭐
                      </span>
                      <span className="text-sm font-semibold">
                        Lore Absorption — capstone
                      </span>
                    </div>
                    <p className="mt-2 text-xs leading-relaxed text-muted-foreground">
                      {weapon.loreAbsorption}
                    </p>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
          </div>
        </div>
      </div>
    </section>
  );
}
