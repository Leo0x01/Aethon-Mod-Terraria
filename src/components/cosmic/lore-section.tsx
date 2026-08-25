"use client";

import { motion } from "framer-motion";
import { LORE } from "@/lib/mod-data";
import { TtsButton } from "./tts-button";

export function LoreSection() {
  return (
    <section id="lore" className="relative py-24 sm:py-32">
      <div className="mx-auto max-w-6xl px-4 sm:px-6">
        <SectionHeading
          kicker="El Lore"
          title={
            <>
              Una historia tejida en{" "}
              <span className="text-glow-violet">luz y vacío</span>
            </>
          }
          subtitle="Antes de que existiera Terraria, ya existía Aethon. Cada estrella es un fragmento suyo. Cada alma, un eco. Esta es la historia que precede a tu viaje."
        />

        <div className="relative mt-16">
          {/* vertical line */}
          <div className="absolute left-4 top-0 h-full w-px bg-gradient-to-b from-primary/0 via-primary/40 to-accent/0 sm:left-1/2 sm:-translate-x-1/2" />

          <ol className="space-y-10 sm:space-y-16">
            {LORE.map((entry, i) => (
              <li
                key={entry.id}
                className={`relative flex flex-col gap-4 pl-12 sm:flex-row sm:items-center sm:gap-8 sm:pl-0 ${
                  i % 2 === 1 ? "sm:flex-row-reverse" : ""
                }`}
              >
                {/* node */}
                <div className="absolute left-4 top-1.5 z-10 -translate-x-1/2 sm:left-1/2">
                  <span className="relative flex h-4 w-4 items-center justify-center">
                    <span className="absolute inset-0 animate-pulse-glow rounded-full bg-primary/40" />
                    <span className="relative h-2.5 w-2.5 rounded-full bg-primary" />
                  </span>
                </div>

                {/* card */}
                <motion.div
                  initial={{ opacity: 0, y: 24 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true, margin: "-80px" }}
                  transition={{ duration: 0.5 }}
                  className={`glass-panel group w-full rounded-2xl border border-border/60 p-6 transition hover:border-primary/40 sm:w-[calc(50%-2.5rem)] ${
                    i % 2 === 1 ? "sm:text-right" : ""
                  }`}
                >
                  <div
                    className={`flex flex-wrap items-center gap-2 ${
                      i % 2 === 1 ? "sm:justify-end" : ""
                    }`}
                  >
                    <span className="font-mono text-[10px] uppercase tracking-[0.2em] text-primary/80">
                      {entry.era}
                    </span>
                    <TtsButton
                      text={`${entry.title}. ${entry.body}`}
                      voice="tongtong"
                      speed={0.95}
                      className="ml-auto sm:ml-0"
                    />
                  </div>
                  <h3 className="mt-1.5 text-xl font-semibold text-glow-gold">
                    {entry.title}
                  </h3>
                  <p className="mt-3 text-sm leading-7 text-muted-foreground">
                    {entry.body}
                  </p>
                </motion.div>

                {/* spacer on desktop for alternating layout */}
                <div className="hidden sm:block sm:w-[calc(50%-2.5rem)]" />
              </li>
            ))}
          </ol>
        </div>
      </div>
    </section>
  );
}

export function SectionHeading({
  kicker,
  title,
  subtitle,
  align = "center",
}: {
  kicker: string;
  title: React.ReactNode;
  subtitle?: string;
  align?: "center" | "left";
}) {
  return (
    <div
      className={
        align === "center"
          ? "mx-auto max-w-2xl text-center"
          : "max-w-2xl text-left"
      }
    >
      <motion.span
        initial={{ opacity: 0, y: 8 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.4 }}
        className="inline-flex items-center gap-2 font-mono text-[11px] uppercase tracking-[0.25em] text-primary/80"
      >
        <span className="h-px w-6 bg-primary/40" />
        {kicker}
        <span className="h-px w-6 bg-primary/40" />
      </motion.span>
      <motion.h2
        initial={{ opacity: 0, y: 12 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true }}
        transition={{ duration: 0.5, delay: 0.05 }}
        className="mt-3 text-balance text-3xl font-bold tracking-tight sm:text-4xl lg:text-5xl"
      >
        {title}
      </motion.h2>
      {subtitle && (
        <motion.p
          initial={{ opacity: 0, y: 12 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true }}
          transition={{ duration: 0.5, delay: 0.12 }}
          className="mt-4 text-balance text-base text-muted-foreground sm:text-lg"
        >
          {subtitle}
        </motion.p>
      )}
    </div>
  );
}
