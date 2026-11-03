export type FeaturedResource = {
  id: string;
  title: string;
  summary: string;
  category: string;
  /** Sample editorial content only — not authoritative accreditation material */
  isSample: true;
};

export const FEATURED_RESOURCES: FeaturedResource[] = [
  {
    id: "sample-explainer-accreditation",
    title: "What accreditation knowledge work involves",
    summary:
      "Sample explainer describing how hospitals prepare evidence and interpret standards — for product layout only.",
    category: "Accreditation explainer",
    isSample: true,
  },
  {
    id: "sample-framework-intro",
    title: "Reading framework structure",
    summary:
      "Sample introduction to chapters, clauses, and measurable elements — not tied to any publisher corpus.",
    category: "Framework introduction",
    isSample: true,
  },
  {
    id: "sample-quality-concepts",
    title: "Continuous improvement in quality systems",
    summary:
      "Sample educational note on PDCA and monitoring concepts — illustrative content for the workspace shell.",
    category: "Quality management",
    isSample: true,
  },
];
