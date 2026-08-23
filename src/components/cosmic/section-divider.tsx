"use client";

/**
 * Decorative cosmic section divider — a horizontal "constellation"
 * line with a glowing center sigil. Used between major sections.
 */
export function SectionDivider({
  variant = "sigil",
}: {
  variant?: "sigil" | "line" | "shards" | "nebula";
}) {
  if (variant === "line") {
    return (
      <div className="relative mx-auto my-2 h-px max-w-5xl">
        <div className="absolute inset-0 bg-gradient-to-r from-transparent via-border to-transparent" />
      </div>
    );
  }

  if (variant === "shards") {
    return (
      <div className="mx-auto flex max-w-5xl items-center justify-center gap-2 py-4">
        {Array.from({ length: 5 }).map((_, i) => (
          <span
            key={i}
            className="h-1 rounded-full bg-gradient-to-r from-primary/0 via-primary/60 to-primary/0"
            style={{
              width: `${10 + i * 8}px`,
              opacity: 0.3 + (i % 2) * 0.3,
            }}
          />
        ))}
      </div>
    );
  }

  if (variant === "nebula") {
    // A wider, softer divider with a dual-tone nebula glow.
    return (
      <div className="relative mx-auto my-2 flex max-w-5xl items-center justify-center gap-3 py-6">
        <span className="h-px flex-1 bg-gradient-to-r from-transparent via-accent/40 to-accent/60" />
        <span className="relative flex h-8 w-8 items-center justify-center">
          <span className="absolute inset-0 animate-pulse-glow rounded-full bg-accent/25 blur-md" />
          <span className="absolute inset-1 rounded-full border border-accent/40" />
          <span className="relative text-accent text-sm">◈</span>
        </span>
        <span className="h-px flex-1 bg-gradient-to-l from-transparent via-primary/40 to-primary/60" />
      </div>
    );
  }

  return (
    <div className="relative mx-auto my-2 flex max-w-5xl items-center justify-center gap-4 py-6">
      <span className="h-px flex-1 bg-gradient-to-r from-transparent to-border/70" />
      <span className="relative flex h-6 w-6 items-center justify-center">
        <span className="absolute inset-0 animate-pulse-glow rounded-full bg-primary/20 blur-md" />
        <span className="relative text-primary">✦</span>
      </span>
      <span className="h-px flex-1 bg-gradient-to-l from-transparent to-border/70" />
    </div>
  );
}
