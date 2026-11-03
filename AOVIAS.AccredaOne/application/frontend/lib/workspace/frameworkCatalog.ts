/** How a framework appears on the workspace landing page (not API wire format). */
export type FrameworkAvailability =
  | "connected_synthetic"
  | "planned"
  | "not_connected";

export type FrameworkCatalogEntry = {
  id: string;
  shortName: string;
  fullName: string;
  description: string;
  availability: FrameworkAvailability;
  /** Eligible discovery framework code when connected via development synthetic pack */
  discoveryCode?: string;
};

/**
 * Product direction includes NABH, JCI, and NABL; only fictional NOAF/MCQF records
 * exist in the current development synthetic discovery dataset.
 */
export const FRAMEWORK_CATALOG: FrameworkCatalogEntry[] = [
  {
    id: "nabh",
    shortName: "NABH",
    fullName: "National Accreditation Board for Hospitals & Healthcare Providers",
    description:
      "Hospital and healthcare accreditation standards for India. Authoritative source content is not connected in this engineering increment.",
    availability: "planned",
  },
  {
    id: "jci",
    shortName: "JCI",
    fullName: "Joint Commission International",
    description:
      "International hospital accreditation standards. Authoritative source content is not connected in this engineering increment.",
    availability: "planned",
  },
  {
    id: "nabl",
    shortName: "NABL",
    fullName: "National Accreditation Board for Testing and Calibration Laboratories",
    description:
      "Laboratory accreditation standards. Authoritative source content is not connected in this engineering increment.",
    availability: "planned",
  },
  {
    id: "noaf",
    shortName: "NOAF",
    fullName: "Northstar Operational Assurance Framework (synthetic)",
    description:
      "Fictional framework records in the development synthetic pack for discovery demos. Not official accreditation content.",
    availability: "connected_synthetic",
    discoveryCode: "NOAF",
  },
  {
    id: "mcqf",
    shortName: "MCQF",
    fullName: "Meridian Care Quality Framework (synthetic)",
    description:
      "Fictional framework records in the development synthetic pack for discovery demos. Not official accreditation content.",
    availability: "connected_synthetic",
    discoveryCode: "MCQF",
  },
];

export function frameworkAvailabilityLabel(availability: FrameworkAvailability): string {
  switch (availability) {
    case "connected_synthetic":
      return "Development synthetic";
    case "planned":
      return "Planned";
    case "not_connected":
      return "Not connected";
  }
}

export function frameworkAvailabilityBadgeClass(availability: FrameworkAvailability): string {
  switch (availability) {
    case "connected_synthetic":
      return "ao-badge ao-badge-info";
    case "planned":
      return "ao-badge ao-badge-warning";
    case "not_connected":
      return "ao-badge";
  }
}
