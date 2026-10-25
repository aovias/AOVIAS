/**
 * Corporate legal identity — single source for footer and schema.
 * Optional registry fields render only when set (do not invent CIN/GSTIN/TAN).
 * Never display PAN.
 */
export const companyLegal = {
  brandName: 'AOVIAS',
  legalName: 'AOVIAS Private Limited',
  tagline: 'Enterprise Application Operations Platform',
  missionLine: 'Making software operation effortless.',
  registeredIn: 'India',
  registeredOffice: [
    'Sri Ram Colony, Gogawan',
    'Khargone, Madhya Pradesh 451335',
    'India',
  ] as const,
  /** Optional — omit or leave empty until counsel provides. */
  cin: undefined as string | undefined,
  gstin: undefined as string | undefined,
  tan: undefined as string | undefined,
  /** Display / general contact. */
  contactEmail: 'admin@aovias.com',
  /** Public enquiry inbox for contact-form delivery. */
  enquiryEmail: 'admin@aovias.com',
  supportEmail: 'nishantsoni11@outlook.com',
  /** Human-readable phone for display. */
  phoneDisplay: '+91 91312 57667',
  /** E.164 for tel: links. */
  phoneTel: '+919131257667',
  website: 'https://aovias.com',
} as const;

export type CompanyLegal = typeof companyLegal;
