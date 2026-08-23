"use client";

import { useEffect, useState } from "react";
import { cn } from "@/lib/utils";

const LINKS = [
  { href: "#lore", label: "Lore" },
  { href: "#weapons", label: "Armas" },
  { href: "#progression", label: "Progresión" },
  { href: "#skill-tree", label: "Árbol" },
  { href: "#codex", label: "Codex" },
  { href: "#bosses", label: "Jefes" },
  { href: "#features", label: "Features" },
];

export function CosmicNav() {
  const [scrolled, setScrolled] = useState(false);
  const [open, setOpen] = useState(false);

  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 40);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  return (
    <header
      className={cn(
        "fixed inset-x-0 top-0 z-50 transition-all duration-300",
        scrolled
          ? "glass-panel border-b border-border/60 backdrop-blur-xl"
          : "bg-transparent",
      )}
    >
      <nav className="mx-auto flex max-w-7xl items-center justify-between px-4 py-3 sm:px-6">
        <a href="#top" className="group flex items-center gap-2.5">
          <span className="relative flex h-9 w-9 items-center justify-center">
            <span className="absolute inset-0 rounded-full bg-primary/30 blur-md transition group-hover:bg-primary/50" />
            <span className="relative flex h-9 w-9 items-center justify-center rounded-full border border-primary/60 bg-card/80 text-primary">
              <span className="text-lg">✦</span>
            </span>
          </span>
          <div className="flex flex-col leading-none">
            <span className="font-mono text-[10px] uppercase tracking-[0.25em] text-muted-foreground">
              Terraria Mod
            </span>
            <span className="text-sm font-semibold text-glow-gold">
              Aethon
            </span>
          </div>
        </a>

        <ul className="hidden items-center gap-1 md:flex">
          {LINKS.map((l) => (
            <li key={l.href}>
              <a
                href={l.href}
                className="rounded-full px-3.5 py-1.5 text-sm text-muted-foreground transition hover:bg-secondary/60 hover:text-foreground"
              >
                {l.label}
              </a>
            </li>
          ))}
        </ul>

        <button
          className="rounded-full border border-border/60 px-4 py-1.5 text-sm text-foreground transition hover:border-primary/60 hover:text-primary md:hidden"
          onClick={() => setOpen((o) => !o)}
          aria-label="Menu"
        >
          ☰
        </button>
      </nav>

      {open && (
        <div className="border-t border-border/40 glass-panel md:hidden">
          <ul className="flex flex-col gap-1 px-4 py-3">
            {LINKS.map((l) => (
              <li key={l.href}>
                <a
                  href={l.href}
                  onClick={() => setOpen(false)}
                  className="block rounded-lg px-3 py-2 text-sm text-muted-foreground transition hover:bg-secondary/60 hover:text-foreground"
                >
                  {l.label}
                </a>
              </li>
            ))}
          </ul>
        </div>
      )}
    </header>
  );
}
