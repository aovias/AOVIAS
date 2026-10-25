export type { IndustryEditorialContent } from './types';
export { IndustryEditorialTemplate } from './industry-editorial-template';
export { healthcareEditorialContent } from './content/healthcare';
export { governmentEditorialContent } from './content/government';
export { manufacturingEditorialContent } from './content/manufacturing';
export { financialServicesEditorialContent } from './content/financial-services';
export { DemoShowcase } from './demo-showcase';
export { ProductExperienceGrid } from './product-experience-grid';
export { ComparisonBand } from './comparison-band';
export { IntegrationOrbit } from './integration-orbit';
export { BrochureBand } from './brochure-band';
export { MidCtaBand } from './mid-cta-band';

import { financialServicesEditorialContent } from './content/financial-services';
import { governmentEditorialContent } from './content/government';
import { healthcareEditorialContent } from './content/healthcare';
import { manufacturingEditorialContent } from './content/manufacturing';
import type { IndustryEditorialContent } from './types';

const editorialBySlug: Record<string, IndustryEditorialContent> = {
  healthcare: healthcareEditorialContent,
  government: governmentEditorialContent,
  manufacturing: manufacturingEditorialContent,
  'financial-services': financialServicesEditorialContent,
};

export function getIndustryEditorial(
  slug: string,
): IndustryEditorialContent | undefined {
  return editorialBySlug[slug];
}

export function getIndustryEditorialSlugs(): string[] {
  return Object.keys(editorialBySlug);
}
