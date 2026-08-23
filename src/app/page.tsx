import { Starfield } from "@/components/cosmic/starfield";
import { CosmicNav } from "@/components/cosmic/nav";
import { Hero } from "@/components/cosmic/hero";
import { LoreSection } from "@/components/cosmic/lore-section";
import { WeaponsSection } from "@/components/cosmic/weapons-section";
import { ProgressionCalculator } from "@/components/cosmic/calculator";
import { SkillTreeView } from "@/components/cosmic/skill-tree";
import { BossesSection } from "@/components/cosmic/bosses-section";
import { FeaturesSection } from "@/components/cosmic/features-section";
import { CosmicFooter } from "@/components/cosmic/footer";

export default function Home() {
  return (
    <div className="relative flex min-h-screen flex-col">
      <Starfield />
      <CosmicNav />
      <main className="relative z-10 flex-1">
        <Hero />
        <LoreSection />
        <WeaponsSection />
        <ProgressionCalculator />
        <SkillTreeView />
        <BossesSection />
        <FeaturesSection />
      </main>
      <CosmicFooter />
    </div>
  );
}
