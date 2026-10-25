import { BrandAtmosphere } from '@/components/brand';

/**
 * Full-bleed atmospheric plane for the homepage hero (AVLS hero-home).
 * Enterprise EBAS + quiet right for signature illustration.
 */
export function HomeHeroAtmosphere() {
  return (
    <BrandAtmosphere
      variant="enterprise"
      quietRight
      density="rich"
    />
  );
}
