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

export default function Home() {
  return (
    <div className="relative flex min-h-screen flex-col">
      <Starfield />
      <ScrollProgress />
      <CosmicNav />
      <main className="relative z-10 flex-1">
        <Hero />
        <LoreSection />
        <SectionDivider variant="sigil" />
        <WeaponsSection />
        <SectionDivider variant="shards" />
        <ProgressionCalculator />
        <SectionDivider variant="sigil" />
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
