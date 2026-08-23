"use client";

import { useState } from "react";
import { motion } from "framer-motion";
import { WEAPONS } from "@/lib/mod-data";
import { BUILD_PRESETS, type BuildPreset } from "@/lib/build-presets";
import { encodeBuild, writeHashBuild } from "@/lib/build-share";
import { SectionHeading } from "./lore-section";

const DIFFICULTY_META: Record<
  BuildPreset["difficulty"],
  { label: string; color: string }
> = {
  easy: { label: "Fácil", color: "#7ee3c4" },
  medium: { label: "Medio", color: "#f5c451" },
  hard: { label: "Difícil", color: "#ff7a7a" },
};

export function BuildPresets() {
  const [loadedId, setLoadedId] = useState<string | null>(null);

  const loadPreset = (preset: BuildPreset) => {
    const weapon = WEAPONS.find((w) => w.id === preset.weaponId)!;
    const ids = weapon.skillTree.map((n) => n.id);
    const enc = encodeBuild(
      preset.weaponId,
      preset.seed,
      preset.nodeIndices.map((idx) => ids[idx]).filter(Boolean),
      ids,
    );
    writeHashBuild(enc);
    // Notify the skill tree + codex to sync to this build.
    window.dispatchEvent(
      new CustomEvent("aethon:build-imported", { detail: { hash: enc } }),
    );
    setLoadedId(preset.id);
    window.setTimeout(() => setLoadedId(null), 2400);
    // Scroll to the skill tree so users see the loaded build.
    document.querySelector("#skill-tree")?.scrollIntoView({ behavior: "smooth" });
  };

  return (
    <section id="presets" className="relative py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <SectionHeading
          kicker="Builds Curadas"
          title={
            <>
              Empieza con un <span className="text-glow-gold">preset</span>
            </>
          }
          subtitle="No sabes por dónde empezar? Carga uno de estos builds de ejemplo con un clic — cada uno demuestra una combinación distinta del árbol de habilidades."
        />

        <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {BUILD_PRESETS.map((preset, i) => {
            const weapon = WEAPONS.find((w) => w.id === preset.weaponId)!;
            const diff = DIFFICULTY_META[preset.difficulty];
            const isLoaded = loadedId === preset.id;
            return (
              <motion.button
                key={preset.id}
                initial={{ opacity: 0, y: 20 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, margin: "-60px" }}
                transition={{ duration: 0.4, delay: (i % 3) * 0.06 }}
                onClick={() => loadPreset(preset)}
                className="group relative overflow-hidden rounded-2xl border bg-card/40 p-5 text-left transition"
                style={
                  isLoaded
                    ? {
                        borderColor: preset.accent,
                        boxShadow: `0 0 28px -6px ${preset.accent}aa`,
                      }
                    : { borderColor: "var(--border)" }
                }
              >
                <div
                  className="pointer-events-none absolute -right-10 -top-10 h-32 w-32 rounded-full opacity-0 blur-3xl transition group-hover:opacity-40"
                  style={{ background: preset.accent }}
                />
                {/* header */}
                <div className="relative flex items-center justify-between">
                  <span
                    className="flex h-10 w-10 items-center justify-center rounded-xl text-lg"
                    style={{
                      background: `${preset.accent}1f`,
                      color: preset.accent,
                      boxShadow: `0 0 20px -6px ${preset.accent}`,
                    }}
                  >
                    {preset.weaponId === "book"
                      ? "📖"
                      : preset.weaponId === "bow"
                        ? "🏹"
                        : preset.weaponId === "sword"
                          ? "⚔"
                          : "🔫"}
                  </span>
                  <span
                    className="rounded-full px-2 py-0.5 font-mono text-[9px] uppercase tracking-wider"
                    style={{
                      background: `${diff.color}1a`,
                      color: diff.color,
                    }}
                  >
                    {diff.label}
                  </span>
                </div>
                {/* title */}
                <h3 className="relative mt-4 text-lg font-semibold">
                  {preset.name}
                </h3>
                <p
                  className="relative mt-0.5 font-mono text-[10px] uppercase tracking-wider"
                  style={{ color: preset.accent }}
                >
                  {preset.archetype} · {weapon.name}
                </p>
                <p className="relative mt-3 text-xs leading-relaxed text-muted-foreground">
                  {preset.description}
                </p>
                {/* footer */}
                <div className="relative mt-4 flex items-center justify-between border-t border-border/40 pt-3">
                  <span className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                    {preset.nodeIndices.length} nodos
                  </span>
                  <span
                    className="inline-flex items-center gap-1 text-xs font-semibold transition group-hover:gap-2"
                    style={{ color: isLoaded ? preset.accent : "var(--primary)" }}
                  >
                    {isLoaded ? "✓ cargado" : "cargar build"}
                    {!isLoaded && <span aria-hidden>→</span>}
                  </span>
                </div>
              </motion.button>
            );
          })}
        </div>
      </div>
    </section>
  );
}

