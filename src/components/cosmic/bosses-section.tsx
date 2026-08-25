"use client";

import { useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { BOSSES, type Boss } from "@/lib/mod-data";
import { SectionHeading } from "./lore-section";
import { TtsButton } from "./tts-button";
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

        {/* Resonance Shard economy summary */}
        <ShardEconomyBanner />

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
                    <div className="mt-0.5 flex flex-wrap items-center gap-x-3 gap-y-0.5 font-mono text-[10px] text-muted-foreground">
                      <span>HP {b.hp}</span>
                      {b.shardDrop > 0 && (
                        <span
                          className="inline-flex items-center gap-1"
                          style={{ color: "var(--primary)" }}
                        >
                          ✦ {b.shardDrop}
                          <span className="text-muted-foreground">shards</span>
                        </span>
                      )}
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
                        <div className="flex items-start gap-3">
                          <p className="flex-1 text-sm leading-relaxed text-muted-foreground">
                            {b.description}
                          </p>
                          <TtsButton
                            text={`${b.name}. ${b.description}`}
                            voice="tongtong"
                            speed={1.0}
                            className="shrink-0"
                          />
                        </div>
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
                        {b.shardDrop > 0 && (
                          <div className="mt-2 flex flex-wrap items-center gap-3 rounded-lg border border-primary/20 bg-primary/5 px-3 py-2 text-xs">
                            <span className="font-mono text-[9px] uppercase tracking-wider text-muted-foreground">
                              Drops:
                            </span>
                            <span className="inline-flex items-center gap-1 text-primary">
                              ✦ {b.shardDrop}
                              <span className="text-muted-foreground">
                                (primera derrota)
                              </span>
                            </span>
                            <span className="inline-flex items-center gap-1 text-muted-foreground">
                              · {b.repeatDrop} ✦
                              <span>(repetibles)</span>
                            </span>
                          </div>
                        )}
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

function ShardEconomyBanner() {
  // Sum across all bosses (first-defeat + repeatable pool).
  const totalFirst = BOSSES.reduce((s, b) => s + b.shardDrop, 0);
  const totalRepeat = BOSSES.reduce((s, b) => s + b.repeatDrop, 0);
  const codexTotal = 880; // sum of all CODEX costs — rough gauge of full unlock
  const pct = Math.min(100, Math.round((totalFirst / codexTotal) * 100));

  return (
    <div className="mt-6 overflow-hidden rounded-2xl border border-primary/30 bg-gradient-to-r from-primary/10 via-transparent to-accent/10 p-4 sm:p-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary/15 text-2xl text-glow-gold">
            ✦
          </span>
          <div>
            <h3 className="text-sm font-semibold">
              Economía de Resonance Shards
            </h3>
            <p className="text-xs text-muted-foreground">
              Moneda para memorizar armas en el Memory Codex.
            </p>
          </div>
        </div>
        <div className="grid grid-cols-3 gap-4 text-center sm:text-right">
          <Stat label="Primera derrota" value={`${totalFirst} ✦`} accent="var(--primary)" />
          <Stat label="Repetible /kill" value={`${totalRepeat} ✦`} accent="var(--accent)" />
          <Stat label="Desbloqueo total" value={`${pct}%`} accent="var(--chart-3)" />
        </div>
      </div>
      <div className="mt-3 h-2 overflow-hidden rounded-full bg-secondary/60">
        <div
          className="h-full rounded-full bg-gradient-to-r from-primary via-amber-300 to-accent transition-all duration-700"
          style={{ width: `${pct}%` }}
        />
      </div>
      <p className="mt-2 font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
        Derrotando cada jefe una vez obtienes ~{pct}% del codex completo
      </p>
    </div>
  );
}

function Stat({
  label,
  value,
  accent,
}: {
  label: string;
  value: string;
  accent: string;
}) {
  return (
    <div>
      <div className="text-lg font-bold tabular-nums" style={{ color: accent }}>
        {value}
      </div>
      <div className="font-mono text-[9px] uppercase tracking-wider text-muted-foreground">
        {label}
      </div>
    </div>
  );
}
