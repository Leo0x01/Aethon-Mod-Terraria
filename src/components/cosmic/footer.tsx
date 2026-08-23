export function CosmicFooter() {
  return (
    <footer className="relative mt-auto border-t border-border/60 bg-card/30 backdrop-blur">
      <div className="pointer-events-none absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-primary/60 to-transparent" />
      <div className="mx-auto max-w-7xl px-4 py-10 sm:px-6">
        <div className="grid gap-8 sm:grid-cols-[1.4fr_1fr_1fr]">
          <div>
            <div className="flex items-center gap-2.5">
              <span className="relative flex h-8 w-8 items-center justify-center rounded-full border border-primary/60 bg-card/80 text-primary">
                ✦
              </span>
              <div className="flex flex-col leading-none">
                <span className="font-mono text-[9px] uppercase tracking-[0.25em] text-muted-foreground">
                  Terraria Mod · Concept
                </span>
                <span className="text-sm font-semibold text-glow-gold">
                  Aethon, the Primordial Light
                </span>
              </div>
            </div>
            <p className="mt-4 max-w-sm text-xs leading-relaxed text-muted-foreground">
              Un concepto de mod cósmico para Terraria (tModLoader). Lore,
              progresión infinita, árboles procedurales y un jefe final que es
              la fuente misma del poder que empuñas.
            </p>
          </div>

          <div>
            <div className="font-mono text-[9px] uppercase tracking-widest text-muted-foreground">
              Secciones
            </div>
            <ul className="mt-3 space-y-1.5 text-sm">
              {[
                { href: "#lore", label: "Lore" },
                { href: "#weapons", label: "Armas" },
                { href: "#progression", label: "Progresión" },
                { href: "#skill-tree", label: "Árbol" },
              ].map((l) => (
                <li key={l.href}>
                  <a
                    href={l.href}
                    className="text-muted-foreground transition hover:text-primary"
                  >
                    {l.label}
                  </a>
                </li>
              ))}
            </ul>
          </div>

          <div>
            <div className="font-mono text-[9px] uppercase tracking-widest text-muted-foreground">
              Stack
            </div>
            <ul className="mt-3 space-y-1.5 text-xs text-muted-foreground">
              <li>Next.js 16 · TypeScript</li>
              <li>Tailwind v4 · shadcn/ui</li>
              <li>Framer Motion · Canvas</li>
              <li>Imágenes: z-ai image-gen</li>
            </ul>
          </div>
        </div>

        <div className="mt-8 flex flex-col items-center justify-between gap-3 border-t border-border/40 pt-6 text-center sm:flex-row sm:text-left">
          <p className="font-mono text-[10px] uppercase tracking-wider text-muted-foreground">
            Concepto fan-made · No afiliado a Re-Logic
          </p>
          <p className="font-mono text-[10px] text-muted-foreground">
            Diseñado por un soñador · Construido por Z.ai Code ✦
          </p>
        </div>
      </div>
    </footer>
  );
}
