"use client";

import { motion } from "framer-motion";
import { SectionHeading } from "./lore-section";

const FEATURES = [
  {
    icon: "∞",
    title: "Nivel infinito",
    desc: "Sin tope. Cada enemigo alimenta el shard. La progresión nunca se detiene — solo se acelera.",
    accent: "#f5c451",
  },
  {
    icon: "🌳",
    title: "Árbol procedural",
    desc: "Generado por seed. Cada personaje obtiene una constelación distinta de habilidades. Re-jugabilidad infinita.",
    accent: "#b388ff",
  },
  {
    icon: "⭐",
    title: "Lore Absorption",
    desc: "El capstone final: tu arma absorbe las habilidades de TODAS las armas de su clase en el juego base y los mods.",
    accent: "#3dd6c4",
  },
  {
    icon: "🌌",
    title: "Eventos cósmicos",
    desc: "Aciagos niveles 25, 50, 75, 100 y 150, el mundo se transforma: lluvia de estrellas, hollowing, rifts dimensionales.",
    accent: "#f5c451",
  },
  {
    icon: "👥",
    title: "Ecos de portadores",
    desc: "Sombras de almas pasadas que también portaron el shard. Jefes opcionales con builds plausibles de tu árbol.",
    accent: "#b388ff",
  },
  {
    icon: "🎨",
    title: "Evolución visual",
    desc: "El fragmento cambia de forma con cada hito: mote tenue → orbe → artefacto rúnico → reliquia estelar → galaxia en miniatura.",
    accent: "#3dd6c4",
  },
  {
    icon: "🎯",
    title: "3 ramas, 3 progresiones",
    desc: "Distancia, Cuerpo a Cuerpo y Artes Mágicas. Cada una con 6 o 7 sub-ramas únicas, 30+ nodos y un capstone de absorción.",
    accent: "#f5c451",
  },
  {
    icon: "🔮",
    title: "Resonancia zonal",
    desc: "El shard gana bonos por bioma: +XP en el espacio, +daño en el inframundo. Cambia tu ruta de farmeo.",
    accent: "#b388ff",
  },
  {
    icon: "💬",
    title: "The Witness NPC",
    desc: "Una entidad que narra lore mientras subes de nivel, vende Memory Runes y — si lo atacas — es superboss.",
    accent: "#3dd6c4",
  },
];

export function FeaturesSection() {
  return (
    <section id="features" className="relative py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-4 sm:px-6">
        <SectionHeading
          kicker="Features"
          title={
            <>
              Nueve sistemas que hacen del mod una{" "}
              <span className="text-glow-violet">experiencia infinita</span>
            </>
          }
          subtitle="No es solo un mod de armas. Es un marco de progresión cósmica que se integra con el mundo, el lore y los jefes existentes."
        />

        <div className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {FEATURES.map((f, i) => (
            <motion.div
              key={f.title}
              initial={{ opacity: 0, y: 20 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, margin: "-60px" }}
              transition={{ duration: 0.4, delay: (i % 3) * 0.06 }}
              className="group relative overflow-hidden rounded-2xl border border-border/60 bg-card/40 p-6 transition hover:border-primary/40"
            >
              <div
                className="pointer-events-none absolute -right-10 -top-10 h-32 w-32 rounded-full opacity-0 blur-3xl transition group-hover:opacity-40"
                style={{ background: f.accent }}
              />
              <div
                className="relative flex h-12 w-12 items-center justify-center rounded-xl text-2xl"
                style={{
                  background: `${f.accent}1f`,
                  color: f.accent,
                  boxShadow: `0 0 24px -8px ${f.accent}`,
                }}
              >
                {f.icon}
              </div>
              <h3 className="relative mt-4 text-lg font-semibold">
                {f.title}
              </h3>
              <p className="relative mt-2 text-sm leading-relaxed text-muted-foreground">
                {f.desc}
              </p>
            </motion.div>
          ))}
        </div>

        {/* CTA strip */}
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.5 }}
          className="mt-14 overflow-hidden rounded-3xl border border-primary/30 bg-gradient-to-br from-primary/10 via-accent/5 to-transparent p-8 text-center sm:p-12"
        >
          <div className="mx-auto max-w-2xl">
            <h3 className="text-2xl font-bold text-glow-gold sm:text-3xl">
              ¿Vincularte al fragmento?
            </h3>
            <p className="mt-3 text-muted-foreground">
              Una vez se imprime la rama de tu fragmento, puedes cambiar más
              adelante invirtiendo Fragmentos de Resonancia — el árbol procedural
              original se conserva como “linaje”. Experimenta las tres
              progresiones en un mismo personaje.
            </p>
            <div className="mt-6 flex flex-wrap justify-center gap-3">
              <a
                href="#weapons"
                className="rounded-full bg-primary px-6 py-3 text-sm font-semibold text-primary-foreground transition hover:box-glow-gold"
              >
                Elegir rama
              </a>
              <a
                href="#skill-tree"
                className="rounded-full border border-border/70 px-6 py-3 text-sm font-semibold transition hover:border-primary/50 hover:text-primary"
              >
                Ver árbol
              </a>
            </div>
          </div>
        </motion.div>
      </div>
    </section>
  );
}
