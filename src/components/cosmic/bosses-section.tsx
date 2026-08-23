"use client";

import { useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { BOSSES, type Boss } from "@/lib/mod-data";
import { SectionHeading } from "./lore-section";
import { cn } from "@/lib/utils";

const TIER_META: Record<
  Boss["tier"],
  { label: string; color: string }
> = {
  mini: { label: "Mini-Boss", color: "#7ee3c4" },
  echo: { label: "Echo", color: "#b388ff" },
  cosmic: { label: "Cósmico", color: "#3dd6c4" },
  final: { label: "Jefe Final", color: "#f5c451" },
};

const FILTERS: { id: "all" | Boss["tier"]; label: string }[] = [
  { id: "all", label: "Todos" },
  { id: "mini", label: "Mini" },
  { id: "echo", label: "Echo" },
  { id: "cosmic", label: "Cósmico" },
  { id: "final", label: "Final" },
];

export function BossesSection() {
  const [filter, setFilter] = useState<"all" | Boss["tier"]>("all");
  const [open, setOpen] = useState<string | null>(BOSSES[0].id);

  const list = BOSSES.filter((b) => filter === "all" || b.tier === filter);

  return (
    <section id="bosses" className="relative py-24 sm:py-32">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <SectionHeading
          kicker="Bestiario Cósmico"
          title={
            <>
              Jefes que{" "}
              <span className="text-glow-gold">doblegan la realidad</span>
            </>
          }
          subtitle="Desde guardianes del Hollow Sanctum hasta ecos de antiguos portadores del shard — y finalmente Aethon mismo, el jefe final de 5 fases."
        />

        <div className="mt-8 flex flex-wrap justify-center gap-2">
          {FILTERS.map((f) => (
            <button
              key={f.id}
              onClick={() => setFilter(f.id)}
              className={cn(
                "rounded-full border px-4 py-1.5 text-xs transition",
                filter === f.id
                  ? "border-primary bg-primary/10 text-primary"
                  : "border-border/60 text-muted-foreground hover:text-foreground",
              )}
            >
              {f.label}
            </button>
          ))}
        </div>

        <div className="mt-10 grid gap-3 sm:grid-cols-2">
          {list.map((b) => {
            const isOpen = open === b.id;
            const meta = TIER_META[b.tier];
            return (
              <motion.div
                key={b.id}
                layout
                className={cn(
                  "overflow-hidden rounded-2xl border bg-card/40 transition",
                  isOpen ? "border-primary/40" : "border-border/50",
                )}
                style={
                  isOpen
                    ? { boxShadow: `0 0 32px -10px ${b.accent}55` }
                    : undefined
                }
              >
                <button
                  onClick={() => setOpen(isOpen ? null : b.id)}
                  className="flex w-full items-center gap-3 p-4 text-left"
                >
                  <span
                    className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl text-xl"
                    style={{
                      background: `${b.accent}22`,
                      color: b.accent,
                      boxShadow: `0 0 18px -6px ${b.accent}88`,
                    }}
                  >
                    {b.tier === "final"
                      ? "✦"
                      : b.tier === "echo"
                        ? "👤"
                        : b.tier === "cosmic"
                          ? "🌌"
                          : "🗿"}
                  </span>
                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2">
                      <span
                        className="font-mono text-[9px] uppercase tracking-wider"
                        style={{ color: meta.color }}
                      >
                        {meta.label}
                      </span>
                      {b.phaseLabel && (
                        <span className="font-mono text-[9px] text-muted-foreground">
                          · {b.phaseLabel}
                        </span>
                      )}
                    </div>
                    <h3 className="mt-0.5 truncate text-base font-semibold text-foreground">
                      {b.name}
                    </h3>
                    <div className="mt-0.5 flex items-center gap-3 font-mono text-[10px] text-muted-foreground">
                      <span>HP {b.hp}</span>
                    </div>
                  </div>
                  <span
                    className={cn(
                      "text-muted-foreground transition",
                      isOpen && "rotate-180",
                    )}
                  >
                    ▾
                  </span>
                </button>

                <AnimatePresence initial={false}>
                  {isOpen && (
                    <motion.div
                      initial={{ height: 0, opacity: 0 }}
                      animate={{ height: "auto", opacity: 1 }}
                      exit={{ height: 0, opacity: 0 }}
                      transition={{ duration: 0.3 }}
                      className="overflow-hidden"
                    >
                      <div className="border-t border-border/40 px-4 pb-4 pt-3">
                        <p className="text-sm leading-relaxed text-muted-foreground">
                          {b.description}
                        </p>
                        <div className="mt-3">
                          <div className="font-mono text-[9px] uppercase tracking-wider text-muted-foreground">
                            Mecánicas
                          </div>
                          <ul className="mt-2 space-y-1.5">
                            {b.mechanics.map((m, i) => (
                              <li
                                key={i}
                                className="flex items-start gap-2 text-xs text-muted-foreground"
                              >
                                <span
                                  className="mt-1.5 h-1 w-1 shrink-0 rounded-full"
                                  style={{ background: b.accent }}
                                />
                                {m}
                              </li>
                            ))}
                          </ul>
                        </div>
                        <div className="mt-3 flex items-center gap-2 rounded-lg border border-border/40 bg-secondary/30 px-3 py-2 text-xs">
                          <span className="font-mono text-[9px] uppercase tracking-wider text-muted-foreground">
                            Desbloqueo:
                          </span>
                          <span className="text-foreground">{b.unlock}</span>
                        </div>
                      </div>
                    </motion.div>
                  )}
                </AnimatePresence>
              </motion.div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
