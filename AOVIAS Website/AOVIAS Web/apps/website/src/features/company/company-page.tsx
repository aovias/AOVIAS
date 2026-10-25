import { AboutSection } from '@/features/company/about/section';
import { ConstitutionSection } from '@/features/company/constitution/section';
import { CultureSection } from '@/features/company/culture/section';
import { CompanyFaqSection } from '@/features/company/faq/section';
import { LeadershipSection } from '@/features/company/leadership/section';
import { MissionSection } from '@/features/company/mission/section';
import { PhilosophySection } from '@/features/company/philosophy/section';
import { StorySection } from '@/features/company/story/section';
import { VisionSection } from '@/features/company/vision/section';

/**
 * Company page composition — orchestrates feature modules only.
 * Each module owns its content and section markup.
 */
export function CompanyPage() {
  return (
    <>
      <AboutSection />
      <StorySection />
      <MissionSection />
      <VisionSection />
      <PhilosophySection />
      <ConstitutionSection />
      <LeadershipSection />
      <CultureSection />
      <CompanyFaqSection />
    </>
  );
}
