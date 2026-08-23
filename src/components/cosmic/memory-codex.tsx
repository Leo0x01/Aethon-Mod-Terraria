"use client";

import { useEffect, useMemo, useState } from "react";
import { AnimatePresence, motion } from "framer-motion";
import {
  CODEX,
  getCodexForClass,
  WEAPONS,
  runeSlotsForLevel,
  type CodexWeapon,
  type WeaponId,
} from "@/lib/mod-data";
import {
  codexIdsFromIndices,
  decodeBuild,
  encodeBuild,
  writeHashBuild,
} from "@/lib/build-share";
import { SectionHeading } from "./lore-section";
import { cn } from "@/lib/utils";

const TIER_META: Record<
  CodexWeapon["tier"],
  { label: string; color: string }
> = {
  pre: { label: "Pre-Hardmode", color: "#7ee3c4" },
  hardmode: { label: "Hardmode", color: "#f5c451" },
  endgame: { label: "Endgame", color: "#b388ff" },
};

function readInitialCodex(): {
  cls: WeaponId;
  memorized: Set<string>;
} {
  if (typeof window === "undefined") {
    return { cls: "book", memorized: new Set() };
  }
  const hash = window.location.hash;
  // Try to decode against every weapon to find the matching shared build.
  for (const w of WEAPONS) {
    const ids = w.skillTree.map((n) => n.id);
    const shared = decodeBuild(hash, ids);
    if (shared && shared.codexIndices.length > 0) {
      const codexIds = codexIdsFromIndices(shared.weaponId, shared.codexIndices);
      return { cls: shared.weaponId, memorized: new Set(codexIds) };
    }
  }
  return { cls: "book", memorized: new Set() };
}

export function MemoryCodex() {
  const [cls, setCls] = useState<WeaponId>("book");
  const [query, setQuery] = useState("");
  const [tier, setTier] = useState<"all" | CodexWeapon["tier"]>("all");
  const [level, setLevel] = useState(100);
  const [memorized, setMemorized] = useState<Set<string>>(new Set());
  const [hydrated, setHydrated] = useState(false);

  // Restore codex loadout from URL hash on mount (client-only, SSR-safe).
  useEffect(() => {
    /* eslint-disable react-hooks/set-state-in-effect */
    const restored = readInitialCodex();
    setCls(restored.cls);
    setMemorized(restored.memorized);
    setHydrated(true);
    /* eslint-enable react-hooks/set-state-in-effect */
  }, []);

  // Persist codex loadout to URL hash (merging with the skill-tree state
  // already in the hash). Only after hydration to avoid clobbering the
  // restored hash with empty defaults pre-hydration.
  useEffect(() => {
    if (!hydrated) return;
    const hash = window.location.hash;
    // Decode the existing skill-tree state from the hash so we preserve it.
    let skillWeapon: WeaponId = cls;
    let skillSeed = 1337;
    let skillNodeIds: string[] = [];
    for (const w of WEAPONS) {
      const ids = w.skillTree.map((n) => n.id);
      const shared = decodeBuild(hash, ids);
      if (shared) {
        skillWeapon = shared.weaponId;
        skillSeed = shared.seed;
        skillNodeIds = shared.nodeIndices.map((i) => ids[i]).filter(Boolean);
        break;
      }
    }
    // If the skill-tree weapon differs from the codex class, we keep them in
    // sync: the codex class follows the skill-tree weapon when a shared build
    // is loaded. Otherwise the codex class is authoritative.
    const effectiveCls = hash ? skillWeapon : cls;
    const allNodeIds = WEAPONS.find((w) => w.id === effectiveCls)!.skillTree.map(
      (n) => n.id,
    );
    const codexIds = [...memorized];
    const enc = encodeBuild(
      effectiveCls,
      skillSeed,
      skillNodeIds,
      allNodeIds,
      codexIds,
    );
    writeHashBuild(enc);
  }, [memorized, cls, level, hydrated]);

  const slots = runeSlotsForLevel(level);
  const list = useMemo(() => {
    return getCodexForClass(cls)
      .filter((c) => tier === "all" || c.tier === tier)
      .filter(
        (c) =>
          !query ||
          c.name.toLowerCase().includes(query.toLowerCase()) ||
          c.signature.toLowerCase().includes(query.toLowerCase()),
      );
  }, [cls, tier, query]);

  const weapon = WEAPONS.find((w) => w.id === cls)!;

  const toggle = (c: CodexWeapon) => {
    setMemorized((prev) => {
      const next = new Set(prev);
      if (next.has(c.id)) {
        next.delete(c.id);
      } else {
        if (next.size >= slots) return prev; // slot cap
        next.add(c.id);
      }
      return next;
    });
  };

  const totalCost = [...memorized]
    .map((id) => CODEX.find((c) => c.id === id)?.cost ?? 0)
    .reduce((a, b) => a + b, 0);

  return (
    <section id="codex" className="relative py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <SectionHeading
          kicker="Memory Codex ⭐ Capstone"
          title={
            <>
              Absorbe{" "}
              <span className="text-glow-violet">cada arma del juego</span>
            </>
          }
          subtitle="El capstone Lore Absorption. Tu Genesis Shard memoriza el comportamiento de armas reales de Terraria — y las equipa simultáneamente como Memory Runes. Explora el codex por clase."
        />

        {/* class tabs */}
        <div className="mt-10 flex flex-wrap justify-center gap-2">
          {WEAPONS.map((w) => (
            <button
              key={w.id}
              onClick={() => {
                setCls(w.id);
                setMemorized(new Set());
              }}
              className={cn(
                "flex items-center gap-2 rounded-full border px-4 py-2 text-sm transition",
                cls === w.id
                  ? "border-transparent"
                  : "border-border/60 text-muted-foreground hover:text-foreground",
              )}
              style={
                cls === w.id
                  ? {
                      borderColor: w.accent,
                      color: w.accent,
                      boxShadow: `0 0 24px -4px ${w.accent}aa`,
                      background: `${w.accent}1a`,
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

        <div className="mt-8 grid grid-cols-1 gap-6 lg:grid-cols-[1.5fr_0.5fr]">
          {/* LEFT: list + filters */}
          <div className="glass-panel rounded-3xl border border-border/60 p-4 sm:p-5">
            {/* filters */}
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div className="relative flex-1 sm:max-w-xs">
                <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground">
                  ⌕
                </span>
                <input
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder={`Buscar en ${list.length} armas...`}
                  className="w-full rounded-full border border-border/60 bg-card/50 py-2 pl-9 pr-3 text-sm placeholder:text-muted-foreground/60 focus:border-primary/50 focus:outline-none focus:ring-1 focus:ring-primary/30"
                />
              </div>
              <div className="flex flex-wrap gap-1.5">
                {(["all", "pre", "hardmode", "endgame"] as const).map((t) => (
                  <button
                    key={t}
                    onClick={() => setTier(t)}
                    className={cn(
                      "rounded-full border px-3 py-1 text-xs transition",
                      tier === t
                        ? "border-primary bg-primary/10 text-primary"
                        : "border-border/60 text-muted-foreground hover:text-foreground",
                    )}
                  >
                    {t === "all" ? "Todas" : TIER_META[t].label}
                  </button>
                ))}
              </div>
            </div>

            {/* level / slots control */}
            <div className="mt-4 flex flex-col gap-3 rounded-2xl border border-border/40 bg-card/30 p-3 sm:flex-row sm:items-center sm:gap-5">
              <div className="flex items-center gap-3">
                <span className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                  Nivel del arma
                </span>
                <input
                  type="range"
                  min={50}
                  max={160}
                  value={level}
                  onChange={(e) => {
                    setLevel(Number(e.target.value));
                    // prune memorized if slots dropped
                    const newSlots = runeSlotsForLevel(Number(e.target.value));
                    if (memorized.size > newSlots) {
                      setMemorized(
                        new Set([...memorized].slice(0, newSlots)),
                      );
                    }
                  }}
                  className="cosmic-range w-32"
                />
                <span className="w-10 font-mono text-sm tabular-nums text-glow-gold">
                  {level}
                </span>
              </div>
              <div className="flex items-center gap-2">
                <span className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                  Slots
                </span>
                <div className="flex gap-1">
                  {Array.from({ length: 5 }).map((_, i) => (
                    <span
                      key={i}
                      className={cn(
                        "h-4 w-4 rounded-full border transition",
                        i < slots
                          ? "border-primary bg-primary/80"
                          : "border-border/50 bg-transparent",
                      )}
                      style={
                        i < slots
                          ? { boxShadow: `0 0 8px ${weapon.accent}` }
                          : undefined
                      }
                    />
                  ))}
                </div>
                <span className="text-sm tabular-nums text-muted-foreground">
                  {slots} / 5
                </span>
              </div>
            </div>

            {/* list */}
            <div className="mt-4 max-h-[560px] space-y-2 overflow-y-auto cosmic-scroll pr-1">
              {list.length === 0 ? (
                <div className="rounded-xl border border-dashed border-border/50 p-8 text-center text-sm text-muted-foreground">
                  Sin resultados. Ajusta tu búsqueda o filtros.
                </div>
              ) : (
                list.map((c) => {
                  const isMem = memorized.has(c.id);
                  const canMem = isMem || memorized.size < slots;
                  return (
                    <button
                      key={c.id}
                      onClick={() => toggle(c)}
                      disabled={!canMem}
                      className={cn(
                        "group flex w-full items-start gap-3 rounded-xl border p-3 text-left transition",
                        isMem
                          ? "border-primary/60 bg-primary/5"
                          : canMem
                            ? "border-border/50 bg-card/30 hover:border-primary/40"
                            : "border-border/30 bg-card/20 opacity-50",
                      )}
                      style={
                        isMem
                          ? { boxShadow: `0 0 20px -8px ${weapon.accent}77` }
                          : undefined
                      }
                    >
                      <span
                        className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-lg text-base font-bold"
                        style={{
                          background: `${TIER_META[c.tier].color}22`,
                          color: TIER_META[c.tier].color,
                        }}
                      >
                        {c.class === "book"
                          ? "📖"
                          : c.class === "bow"
                            ? "🏹"
                            : c.class === "sword"
                              ? "⚔"
                              : "🔫"}
                      </span>
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <span className="text-sm font-semibold">
                            {c.name}
                          </span>
                          <span
                            className="rounded-full px-1.5 py-0.5 font-mono text-[9px] uppercase tracking-wider"
                            style={{
                              background: `${TIER_META[c.tier].color}1a`,
                              color: TIER_META[c.tier].color,
                            }}
                          >
                            {TIER_META[c.tier].label}
                          </span>
                        </div>
                        <div className="mt-1 text-xs text-muted-foreground">
                          <span className="text-foreground">
                            {c.signature}:
                          </span>{" "}
                          {c.effect}
                        </div>
                        <div className="mt-1.5 flex items-center gap-3 font-mono text-[10px] text-muted-foreground/80">
                          <span className="text-primary/90">costo {c.cost} ✦</span>
                          <span className="truncate">· {c.source}</span>
                        </div>
                      </div>
                      <span
                        className={cn(
                          "mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full border text-xs transition",
                          isMem
                            ? "border-primary bg-primary text-primary-foreground"
                            : "border-border/50 text-muted-foreground",
                        )}
                      >
                        {isMem ? "✓" : "+"}
                      </span>
                    </button>
                  );
                })
              )}
            </div>
          </div>

          {/* RIGHT: rune loadout */}
          <div className="flex flex-col gap-4">
            <div className="glass-panel rounded-3xl border border-border/60 p-5">
              <div className="flex items-center justify-between">
                <span className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                  Memory Runes
                </span>
                <span className="font-mono text-[10px] text-muted-foreground">
                  {memorized.size}/{slots}
                </span>
              </div>

              {/* slot grid */}
              <div className="mt-3 grid grid-cols-1 gap-2">
                {Array.from({ length: slots }).map((_, i) => {
                  const id = [...memorized][i];
                  const c = id ? CODEX.find((x) => x.id === id) : null;
                  return (
                    <div
                      key={i}
                      className={cn(
                        "rounded-xl border p-2.5 transition",
                        c
                          ? "border-primary/50 bg-primary/5"
                          : "border-dashed border-border/50",
                      )}
                    >
                      {c ? (
                        <div className="flex items-center gap-2">
                          <span
                            className="flex h-7 w-7 items-center justify-center rounded-md text-xs"
                            style={{
                              background: `${TIER_META[c.tier].color}22`,
                              color: TIER_META[c.tier].color,
                            }}
                          >
                            {c.class === "book"
                              ? "📖"
                              : c.class === "bow"
                                ? "🏹"
                                : c.class === "sword"
                                  ? "⚔"
                                  : "🔫"}
                          </span>
                          <div className="min-w-0 flex-1">
                            <div className="truncate text-xs font-semibold">
                              {c.name}
                            </div>
                            <div className="truncate font-mono text-[9px] text-muted-foreground">
                              {c.signature}
                            </div>
                          </div>
                          <button
                            onClick={() => toggle(c)}
                            className="text-muted-foreground transition hover:text-destructive"
                            aria-label={`Quitar ${c.name}`}
                          >
                            ✕
                          </button>
                        </div>
                      ) : (
                        <div className="flex items-center justify-center py-1 font-mono text-[10px] uppercase tracking-wider text-muted-foreground/50">
                          slot vacío
                        </div>
                      )}
                    </div>
                  );
                })}
                {slots === 0 && (
                  <div className="rounded-xl border border-dashed border-border/50 p-4 text-center text-xs text-muted-foreground">
                    Desbloquea slots al llegar a Lv 50.
                  </div>
                )}
              </div>

              <div className="mt-4 flex items-center justify-between border-t border-border/40 pt-3">
                <span className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
                  Resonancia invertida
                </span>
                <span className="font-mono text-sm tabular-nums text-glow-gold">
                  {totalCost} ✦
                </span>
              </div>
            </div>

            {/* equipped effects summary */}
            <div className="glass-panel rounded-3xl border border-border/60 p-5">
              <div className="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                Efectos activos
              </div>
              <AnimatePresence mode="popLayout">
                {memorized.size === 0 ? (
                  <motion.p
                    key="empty"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    className="mt-2 text-xs text-muted-foreground"
                  >
                    Ninguna rune equipada. El arma funciona con su base.
                  </motion.p>
                ) : (
                  <ul className="mt-2 space-y-1.5">
                    {[...memorized].map((id) => {
                      const c = CODEX.find((x) => x.id === id)!;
                      return (
                        <motion.li
                          key={id}
                          layout
                          initial={{ opacity: 0, x: 10 }}
                          animate={{ opacity: 1, x: 0 }}
                          exit={{ opacity: 0, x: -10 }}
                          className="flex items-start gap-1.5 text-xs"
                        >
                          <span
                            className="mt-1 h-1 w-1 shrink-0 rounded-full"
                            style={{ background: weapon.accent }}
                          />
                          <span className="text-muted-foreground">
                            <span className="text-foreground">{c.signature}</span>
                            : {c.effect}
                          </span>
                        </motion.li>
                      );
                    })}
                  </ul>
                )}
              </AnimatePresence>
            </div>

            <div className="rounded-2xl border border-border/40 bg-card/20 p-4 text-xs text-muted-foreground">
              <p className="font-mono uppercase tracking-wider text-foreground/70">
                Cómo funciona
              </p>
              <ul className="mt-2 space-y-1.5">
                <li>• Sube de nivel el arma para desbloquear slots (Lv 50+).</li>
                <li>• Memoriza armas del juego base como Runes.</li>
                <li>• Las Runes equipadas se manifiestan simultáneamente.</li>
                <li>• Resonancia Shards se obtienen de jefes cósmicos.</li>
              </ul>
              {memorized.size > 0 && (
                <p className="mt-3 border-t border-border/40 pt-2 font-mono text-[10px] uppercase tracking-wider text-primary/80">
                  ✓ loadout incluido en el enlace compartido
                </p>
              )}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
