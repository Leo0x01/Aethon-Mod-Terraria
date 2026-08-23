"use client";

import { useEffect, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import { WEAPONS } from "@/lib/mod-data";
import {
  BUILD_PRESETS,
  deleteUserPreset,
  loadUserPresets,
  saveUserPreset,
  type BuildPreset,
  type UserPreset,
} from "@/lib/build-presets";
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
  const [userPresets, setUserPresets] = useState<UserPreset[]>([]);
  const [saveName, setSaveName] = useState("");
  const [announce, setAnnounce] = useState("");
  const [hydrated, setHydrated] = useState(false);

  // Load user presets from localStorage on mount (SSR-safe).
  useEffect(() => {
    /* eslint-disable react-hooks/set-state-in-effect */
    setUserPresets(loadUserPresets());
    setHydrated(true);
    /* eslint-enable react-hooks/set-state-in-effect */
  }, []);

  const announceMsg = (msg: string) => {
    setAnnounce(msg);
    window.setTimeout(() => setAnnounce(""), 3000);
  };

  const loadHash = (hash: string, id: string) => {
    writeHashBuild(hash);
    window.dispatchEvent(
      new CustomEvent("aethon:build-imported", { detail: { hash } }),
    );
    setLoadedId(id);
    window.setTimeout(() => setLoadedId(null), 2400);
    announceMsg("Build cargado");
    document.querySelector("#skill-tree")?.scrollIntoView({ behavior: "smooth" });
  };

  const loadPreset = (preset: BuildPreset) => {
    const weapon = WEAPONS.find((w) => w.id === preset.weaponId)!;
    const ids = weapon.skillTree.map((n) => n.id);
    const enc = encodeBuild(
      preset.weaponId,
      preset.seed,
      preset.nodeIndices.map((idx) => ids[idx]).filter(Boolean),
      ids,
    );
    loadHash(enc, preset.id);
  };

  const onSaveCurrent = () => {
    // Read the current build hash from the URL. The skill-tree + codex persist
    // effects keep this in sync with the live build state.
    const rawHash =
      typeof window !== "undefined" ? window.location.hash : "";
    const hash = rawHash.replace(/^#/, "");
    if (!hash || (!hash.startsWith("v1.") && !hash.startsWith("v2."))) {
      announceMsg("No hay build actual para guardar");
      return;
    }
    const name = saveName.trim() || `Build ${userPresets.length + 1}`;
    saveUserPreset(name, hash);
    setUserPresets(loadUserPresets());
    setSaveName("");
    announceMsg(`"${name}" guardado`);
  };

  const onDeleteUser = (id: string) => {
    deleteUserPreset(id);
    setUserPresets(loadUserPresets());
    announceMsg("Build eliminado");
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

        {/* aria-live region for screen-reader announcements */}
        <div aria-live="polite" className="sr-only">
          {announce}
        </div>

        {/* Save current build panel */}
        <div className="mt-10 flex flex-col gap-3 rounded-2xl border border-primary/30 bg-gradient-to-r from-primary/10 via-transparent to-accent/5 p-4 sm:flex-row sm:items-center sm:gap-4">
          <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary/15 text-xl text-glow-gold">
            💾
          </span>
          <div className="flex-1">
            <h3 className="text-sm font-semibold">Guardar build actual</h3>
            <p className="text-xs text-muted-foreground">
              Guarda tu build del árbol + codex para reutilizarla después.
            </p>
          </div>
          <input
            value={saveName}
            onChange={(e) => setSaveName(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") onSaveCurrent();
            }}
            placeholder="nombre del build…"
            maxLength={40}
            className="w-full rounded-full border border-border/60 bg-background/60 px-4 py-2 text-xs text-foreground placeholder:text-muted-foreground/60 focus:border-primary/50 focus:outline-none focus:ring-1 focus:ring-primary/30 sm:w-48"
            aria-label="Nombre del build a guardar"
          />
          <button
            onClick={onSaveCurrent}
            className="rounded-full bg-primary px-5 py-2 text-xs font-semibold text-primary-foreground transition hover:opacity-90"
          >
            guardar
          </button>
        </div>

        {/* Curated presets grid */}
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

        {/* User-saved presets */}
        {hydrated && userPresets.length > 0 && (
          <div className="mt-12">
            <h3 className="mb-4 flex items-center gap-2 text-sm font-semibold">
              <span className="text-glow-gold">★</span> Mis builds guardados
              <span className="rounded-full bg-secondary/60 px-2 py-0.5 font-mono text-[10px] text-muted-foreground">
                {userPresets.length}
              </span>
            </h3>
            <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <AnimatePresence>
                {userPresets.map((up) => {
                  const isLoaded = loadedId === up.id;
                  return (
                    <motion.div
                      key={up.id}
                      layout
                      initial={{ opacity: 0, scale: 0.9 }}
                      animate={{ opacity: 1, scale: 1 }}
                      exit={{ opacity: 0, scale: 0.9 }}
                      transition={{ duration: 0.2 }}
                      className="group relative overflow-hidden rounded-xl border border-border/60 bg-card/40 p-4"
                      style={
                        isLoaded
                          ? {
                              borderColor: "var(--primary)",
                              boxShadow: "0 0 20px -6px var(--primary)",
                            }
                          : undefined
                      }
                    >
                      <div className="flex items-start justify-between gap-2">
                        <button
                          onClick={() => loadHash(up.hash, up.id)}
                          className="min-w-0 flex-1 text-left"
                        >
                          <h4 className="truncate text-sm font-semibold">
                            {up.name}
                          </h4>
                          <p className="mt-0.5 font-mono text-[9px] uppercase tracking-wider text-muted-foreground">
                            {new Date(up.createdAt).toLocaleDateString()}
                          </p>
                        </button>
                        <button
                          onClick={() => onDeleteUser(up.id)}
                          className="shrink-0 rounded-full px-2 py-1 text-xs text-muted-foreground transition hover:bg-destructive/10 hover:text-destructive"
                          aria-label={`Eliminar ${up.name}`}
                        >
                          ✕
                        </button>
                      </div>
                      <button
                        onClick={() => loadHash(up.hash, up.id)}
                        className="mt-3 flex w-full items-center justify-between rounded-lg border border-border/40 px-3 py-1.5 text-xs font-semibold text-primary transition hover:border-primary/60 hover:bg-primary/5"
                      >
                        {isLoaded ? "✓ cargado" : "cargar"}
                        {!isLoaded && <span aria-hidden>→</span>}
                      </button>
                    </motion.div>
                  );
                })}
              </AnimatePresence>
            </div>
          </div>
        )}

        {/* Toast announcement (visible) */}
        <AnimatePresence>
          {announce && (
            <motion.div
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: 20 }}
              className="pointer-events-none fixed bottom-20 left-1/2 z-50 -translate-x-1/2 rounded-full border border-primary/40 bg-card/95 px-5 py-2 text-sm font-medium text-primary shadow-2xl backdrop-blur"
            >
              {announce}
            </motion.div>
          )}
        </AnimatePresence>
      </div>
    </section>
  );
}
