import { Starfield } from "@/components/cosmic/starfield";
import { ScrollProgress } from "@/components/cosmic/scroll-progress";
import { CosmicNav } from "@/components/cosmic/nav";
import { Hero } from "@/components/cosmic/hero";
import { LoreSection } from "@/components/cosmic/lore-section";
import { WeaponsSection } from "@/components/cosmic/weapons-section";
import { ProgressionCalculator } from "@/components/cosmic/calculator";
import { SkillTreeView } from "@/components/cosmic/skill-tree";
import { MemoryCodex } from "@/components/cosmic/memory-codex";
import { BossesSection } from "@/components/cosmic/bosses-section";
import { FeaturesSection } from "@/components/cosmic/features-section";
import { CosmicFooter } from "@/components/cosmic/footer";
import { SectionDivider } from "@/components/cosmic/section-divider";
import { BackToTop } from "@/components/cosmic/back-to-top";
import { BuildPresets } from "@/components/cosmic/build-presets";

export default function Home() {
  return (
    <div className="relative flex min-h-screen flex-col">
      <Starfield />
      <ScrollProgress />
      {/* Skip-to-content link for screen readers / keyboard users */}
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:z-[100] focus:rounded-full focus:border focus:border-primary/60 focus:bg-card focus:px-4 focus:py-2 focus:text-sm focus:text-primary"
      >
        Saltar al contenido
      </a>
      <CosmicNav />
      <main id="main-content" className="relative z-10 flex-1">
        <Hero />
        <LoreSection />
        <SectionDivider variant="sigil" />
        <WeaponsSection />
        <SectionDivider variant="shards" />
        <ProgressionCalculator />
        <SectionDivider variant="sigil" />
        <BuildPresets />
        <SectionDivider variant="shards" />
        <SkillTreeView />
        <SectionDivider variant="shards" />
        <MemoryCodex />
        <SectionDivider variant="nebula" />
        <BossesSection />
        <SectionDivider variant="sigil" />
        <FeaturesSection />
      </main>
      <CosmicFooter />
      <BackToTop />
    </div>
  );
}
