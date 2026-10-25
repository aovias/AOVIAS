import { BenefitsSection } from '@/features/careers/benefits/section';
import { CareersCtaSection } from '@/features/careers/cta/section';
import { CareersCultureSection } from '@/features/careers/culture/section';
import { CareersHeroSection } from '@/features/careers/hero/section';
import { HiringProcessSection } from '@/features/careers/hiring-process/section';
import { OpenRolesSection } from '@/features/careers/open-roles/section';
import { WhyAoviasSection } from '@/features/careers/why-aovias/section';

/**
 * Careers page composition — Marketing Layout sections only.
 */
export function CareersPage() {
  return (
    <>
      <CareersHeroSection />
      <CareersCultureSection />
      <WhyAoviasSection />
      <BenefitsSection />
      <HiringProcessSection />
      <OpenRolesSection />
      <CareersCtaSection />
    </>
  );
}
