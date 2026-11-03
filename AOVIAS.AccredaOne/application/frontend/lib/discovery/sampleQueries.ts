/** Sample questions grounded in the fictional NOAF/MCQF development synthetic dataset. */
export type DiscoverySampleQuery = {
  id: string;
  label: string;
  hint: string;
  query: string;
};

export const DISCOVERY_SAMPLE_QUERIES: DiscoverySampleQuery[] = [
  {
    id: "cross-framework",
    label: "Cross-framework overview",
    hint: "Leaves framework empty — searches all eligible synthetic frameworks (NOAF and MCQF).",
    query:
      "Show synthetic requirements across the fictional NOAF and MCQF development frameworks.",
  },
  {
    id: "noaf-section-a",
    label: "NOAF section-a assurance",
    hint: "Matches NOAF synthetic records for section-a operational and continuity themes.",
    query:
      "Which fictional NOAF section-a requirements describe operational assurance and continuity planning?",
  },
  {
    id: "mcqf-chapter-1",
    label: "MCQF chapter 1",
    hint: "Matches MCQF synthetic records for chapter-1 patient experience and governance.",
    query:
      "What synthetic MCQF chapter-1 requirements cover patient experience and clinical governance?",
  },
  {
    id: "mcqf-chapter-2",
    label: "MCQF chapter 2",
    hint: "Matches MCQF synthetic records for chapter-2 medication safety and workforce competency.",
    query:
      "Which fictional MCQF chapter-2 requirements relate to medication safety and workforce competency?",
  },
];
