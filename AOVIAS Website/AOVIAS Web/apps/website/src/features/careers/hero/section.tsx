import { DomainScene } from '@/components/brand';
import { Hero } from '@/components/marketing';
import { heroContent, heroMeta } from '@/features/careers/hero/content';

/**
 * Careers hero — purpose before positions.
 */
export function CareersHeroSection() {
  return (
    <Hero
      eyebrow={heroContent.eyebrow}
      title={heroContent.title}
      description={heroContent.description}
      primaryCta={heroContent.primaryCta}
      secondaryCta={heroContent.secondaryCta}
      atmosphere="enterprise-abstract"
      illustration={
        <DomainScene
          title={`${heroMeta.title} illustration`}
          caption={heroContent.illustrationLabel}
          variant="careers"
        />
      }
    />
  );
}
