"use client";

import { useEffect, useState } from "react";
import { motion, useScroll, useSpring } from "framer-motion";

const SECTIONS = [
  { id: "top", label: "Inicio" },
  { id: "lore", label: "Lore" },
  { id: "weapons", label: "Armas" },
  { id: "progression", label: "Progresión" },
  { id: "skill-tree", label: "Árbol" },
  { id: "codex", label: "Codex" },
  { id: "bosses", label: "Jefes" },
  { id: "features", label: "Features" },
];

export function ScrollProgress() {
  const { scrollYProgress } = useScroll();
  const scaleX = useSpring(scrollYProgress, {
    stiffness: 120,
    damping: 30,
    restDelta: 0.001,
  });
  const [active, setActive] = useState("top");
  const [scrolled, setScrolled] = useState(false);

  useEffect(() => {
    const onScroll = () => {
      setScrolled(window.scrollY > 80);
      const mid = window.scrollY + window.innerHeight * 0.35;
      let current = "top";
      for (const s of SECTIONS) {
        const el = document.getElementById(s.id);
        if (el && el.offsetTop <= mid) current = s.id;
      }
      setActive(current);
    };
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <>
      {/* top progress bar */}
      <motion.div
        style={{ scaleX }}
        className="fixed inset-x-0 top-0 z-[60] h-0.5 origin-left bg-gradient-to-r from-primary via-amber-300 to-accent"
      />

      {/* side dot navigator (desktop only) */}
      <div
        className={`pointer-events-none fixed right-4 top-1/2 z-40 hidden -translate-y-1/2 flex-col items-end gap-2 transition-opacity duration-300 xl:flex ${
          scrolled ? "opacity-70" : "opacity-0"
        } hover:!opacity-100`}
      >
        {SECTIONS.map((s) => {
          const isActive = active === s.id;
          return (
            <a
              key={s.id}
              href={`#${s.id}`}
              className="pointer-events-auto group flex items-center gap-2"
            >
              <span
                className={`font-mono text-[10px] uppercase tracking-wider transition-all duration-300 ${
                  isActive
                    ? "text-primary opacity-100"
                    : "text-muted-foreground opacity-0 group-hover:opacity-100"
                }`}
              >
                {s.label}
              </span>
              <span
                className={`block rounded-full transition-all duration-300 ${
                  isActive
                    ? "h-2.5 w-2.5 bg-primary shadow-[0_0_10px_currentColor]"
                    : "h-1.5 w-1.5 bg-muted-foreground/50 group-hover:bg-foreground"
                }`}
              />
            </a>
          );
        })}
      </div>
    </>
  );
}
