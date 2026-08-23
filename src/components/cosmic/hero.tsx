"use client";

import Image from "next/image";
import { motion } from "framer-motion";
import { WEAPONS } from "@/lib/mod-data";

export function Hero() {
  return (
    <section
      id="top"
      className="relative flex min-h-screen items-center overflow-hidden pt-24 pb-16"
    >
      {/* orbiting rings */}
      <div className="pointer-events-none absolute left-1/2 top-1/2 -z-0 -translate-x-1/2 -translate-y-1/2">
        <div className="animate-orbit h-[520px] w-[520px] rounded-full border border-primary/10 sm:h-[760px] sm:w-[760px]" />
        <div className="animate-orbit-rev absolute inset-0 m-auto h-[420px] w-[420px] rounded-full border border-accent/10 sm:h-[620px] sm:w-[620px]" />
        <div className="animate-orbit absolute inset-0 m-auto h-[320px] w-[320px] rounded-full border border-primary/10 sm:h-[460px] sm:w-[460px]" />
      </div>

      <div className="relative z-10 mx-auto grid w-full max-w-7xl items-center gap-12 px-4 sm:px-6 lg:grid-cols-[1.1fr_0.9fr]">
        {/* LEFT: copy */}
        <div>
          <motion.span
            initial={{ opacity: 0, y: 12 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6 }}
            className="inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/5 px-3 py-1 font-mono text-[11px] uppercase tracking-[0.2em] text-primary/90"
          >
            <span className="h-1.5 w-1.5 animate-pulse-glow rounded-full bg-primary" />
            Concepto de Mod Cósmico · tModLoader
          </motion.span>

          <motion.h1
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.08 }}
            className="mt-5 text-balance text-4xl font-bold leading-[1.05] tracking-tight sm:text-6xl lg:text-7xl"
          >
            <span className="text-glow-gold">Aethon</span>
            <span className="text-muted-foreground">,</span>
            <br />
            the Primordial
            <br />
            <span className="bg-gradient-to-r from-primary via-amber-200 to-accent bg-clip-text text-transparent">
              Light
            </span>
          </motion.h1>

          <motion.p
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.16 }}
            className="mt-6 max-w-xl text-base leading-relaxed text-muted-foreground sm:text-lg"
          >
            Encuentra un altar antiguo. Bondéate a un{" "}
            <span className="text-foreground">fragmento de luz</span> de una
            entidad cósmica. Sube de nivel{" "}
            <span className="text-primary">infinitamente</span> matando
            enemigos, desbloquea un árbol de habilidades{" "}
            <span className="text-accent">generado proceduralmente</span> y
            persigue al jefe final que vive dentro de tu propia arma.
          </motion.p>

          <motion.div
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.24 }}
            className="mt-8 flex flex-wrap items-center gap-3"
          >
            <a
              href="#weapons"
              className="group relative inline-flex items-center gap-2 overflow-hidden rounded-full bg-primary px-6 py-3 text-sm font-semibold text-primary-foreground transition hover:box-glow-gold"
            >
              <span className="absolute inset-0 -translate-x-full bg-gradient-to-r from-transparent via-white/30 to-transparent transition-transform duration-700 group-hover:translate-x-full" />
              Elegir tu arma
              <span aria-hidden>→</span>
            </a>
            <a
              href="#lore"
              className="inline-flex items-center gap-2 rounded-full border border-border/70 px-6 py-3 text-sm font-semibold text-foreground transition hover:border-primary/50 hover:text-primary"
            >
              Leer el lore
            </a>
          </motion.div>

          <motion.div
            initial={{ opacity: 0, y: 18 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.32 }}
            className="mt-10 grid max-w-md grid-cols-3 gap-4 text-center"
          >
            {[
              { v: "∞", l: "Niveles" },
              { v: "4", l: "Armas únicas" },
              { v: "6", l: "Ramas por arma" },
            ].map((s) => (
              <div
                key={s.l}
                className="rounded-xl border border-border/50 bg-card/40 px-2 py-3"
              >
                <div className="text-2xl font-bold text-glow-gold">{s.v}</div>
                <div className="mt-0.5 font-mono text-[10px] uppercase tracking-widest text-muted-foreground">
                  {s.l}
                </div>
              </div>
            ))}
          </motion.div>
        </div>

        {/* RIGHT: entity image */}
        <motion.div
          initial={{ opacity: 0, scale: 0.92 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.9, delay: 0.1 }}
          className="relative mx-auto aspect-square w-full max-w-md"
        >
          <div className="absolute inset-0 rounded-full bg-primary/10 blur-3xl" />
          <div className="absolute inset-0 animate-pulse-glow rounded-full bg-accent/15 blur-2xl" />
          <Image
            src="/cosmic/entity.png"
            alt="Aethon, the Primordial Light — cosmic entity"
            fill
            priority
            sizes="(max-width: 768px) 90vw, 480px"
            className="relative animate-float-slow rounded-[2rem] object-cover"
          />
          {/* floating weapon chips */}
          <div className="absolute -left-2 top-8 hidden rotate-[-8deg] sm:block">
            <WeaponChip id="bow" />
          </div>
          <div className="absolute -right-2 top-1/3 hidden rotate-[6deg] sm:block">
            <WeaponChip id="book" />
          </div>
          <div className="absolute bottom-6 left-6 hidden rotate-[4deg] sm:block">
            <WeaponChip id="sword" />
          </div>
          <div className="absolute bottom-10 right-4 hidden rotate-[-5deg] sm:block">
            <WeaponChip id="cannon" />
          </div>
        </motion.div>
      </div>

      <div className="absolute inset-x-0 bottom-0 h-24 bg-gradient-to-t from-background to-transparent" />
    </section>
  );
}

function WeaponChip({ id }: { id: (typeof WEAPONS)[number]["id"] }) {
  const w = WEAPONS.find((x) => x.id === id)!;
  return (
    <div
      className="glass-panel flex items-center gap-2 rounded-full border border-border/70 px-3 py-1.5 text-xs shadow-lg"
      style={{ boxShadow: `0 0 24px -8px ${w.accent}55` }}
    >
      <span
        className="h-2 w-2 rounded-full"
        style={{ background: w.accent, boxShadow: `0 0 10px ${w.accent}` }}
      />
      <span className="font-medium text-foreground">{w.name}</span>
      <span className="text-muted-foreground">{w.epithet}</span>
    </div>
  );
}
