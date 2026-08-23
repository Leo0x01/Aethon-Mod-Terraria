"use client";

import Image from "next/image";
import {
  motion,
  useMotionTemplate,
  useMotionValue,
  useSpring,
  useTransform,
} from "framer-motion";
import { BRANCHES } from "@/lib/mod-data";

export function Hero() {
  return (
    <section
      id="top"
      className="relative flex min-h-screen items-center overflow-hidden pt-24 pb-16"
    >
      {/* orbiting rings (hidden on mobile to avoid overflow) */}
      <div className="pointer-events-none absolute left-1/2 top-1/2 -z-0 hidden -translate-x-1/2 -translate-y-1/2 sm:block">
        <div className="animate-orbit h-[520px] w-[520px] rounded-full border border-primary/10 sm:h-[760px] sm:w-[760px]" />
        <div className="animate-orbit-rev absolute inset-0 m-auto h-[420px] w-[420px] rounded-full border border-accent/10 sm:h-[620px] sm:w-[620px]" />
        <div className="animate-orbit absolute inset-0 m-auto h-[320px] w-[320px] rounded-full border border-primary/10 sm:h-[460px] sm:w-[460px]" />
      </div>

      <div className="relative z-10 mx-auto grid w-full max-w-7xl grid-cols-1 items-center gap-12 px-4 sm:px-6 lg:grid-cols-[1.1fr_0.9fr]">
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
            Encuentra un altar antiguo. Vincúlate a un{" "}
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
              Elegir tu rama
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
              { v: "3", l: "Ramas únicas" },
              { v: "6+", l: "Sub-ramas por árbol" },
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
        <HeroEntity />
      </div>

      <div className="absolute inset-x-0 bottom-0 h-24 bg-gradient-to-t from-background to-transparent" />
    </section>
  );
}

function HeroEntity() {
  // Mouse-parallax tilt: the entity image leans toward the cursor for depth.
  const mx = useMotionValue(0);
  const my = useMotionValue(0);
  const rotateX = useSpring(useTransform(my, [-0.5, 0.5], [8, -8]), {
    stiffness: 120,
    damping: 18,
  });
  const rotateY = useSpring(useTransform(mx, [-0.5, 0.5], [-8, 8]), {
    stiffness: 120,
    damping: 18,
  });
  const glowX = useTransform(mx, [-0.5, 0.5], ["30%", "70%"]);
  const glowY = useTransform(my, [-0.5, 0.5], ["30%", "70%"]);

  const onMove = (e: React.MouseEvent<HTMLDivElement>) => {
    const rect = e.currentTarget.getBoundingClientRect();
    mx.set((e.clientX - rect.left) / rect.width - 0.5);
    my.set((e.clientY - rect.top) / rect.height - 0.5);
  };
  const onLeave = () => {
    mx.set(0);
    my.set(0);
  };

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.92 }}
      animate={{ opacity: 1, scale: 1 }}
      transition={{ duration: 0.9, delay: 0.1 }}
      onMouseMove={onMove}
      onMouseLeave={onLeave}
      style={{ perspective: 1000 }}
      className="relative mx-auto aspect-square w-full max-w-md"
    >
      <div className="absolute inset-0 rounded-full bg-primary/10 blur-3xl" />
      <div className="absolute inset-0 animate-pulse-glow rounded-full bg-accent/15 blur-2xl" />
      {/* cursor-following glow */}
      <motion.div
        className="pointer-events-none absolute inset-0 rounded-[2rem] opacity-60"
        style={{
          background: useMotionTemplate`radial-gradient(circle at ${glowX} ${glowY}, rgba(245,196,81,0.35), transparent 55%)`,
        }}
      />
      <motion.div
        style={{ rotateX, rotateY, transformStyle: "preserve-3d" }}
        className="relative h-full w-full"
      >
        <Image
          src="/cosmic/entity.png"
          alt="Aethon, the Primordial Light — cosmic entity"
          fill
          priority
          sizes="(max-width: 768px) 90vw, 480px"
          className="animate-float-slow rounded-[2rem] object-cover"
        />
      </motion.div>
      {/* floating branch chips */}
      <div className="absolute -left-2 top-8 hidden rotate-[-8deg] sm:block">
        <WeaponChip id="distance" />
      </div>
      <div className="absolute -right-2 top-1/3 hidden rotate-[6deg] sm:block">
        <WeaponChip id="magic" />
      </div>
      <div className="absolute bottom-6 left-6 hidden rotate-[4deg] sm:block">
        <WeaponChip id="melee" />
      </div>
    </motion.div>
  );
}

function WeaponChip({ id }: { id: (typeof BRANCHES)[number]["id"] }) {
  const w = BRANCHES.find((x) => x.id === id)!;
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
