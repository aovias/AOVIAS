export interface RoleGuidance {
  welcomeSubtitle: string;
  nextHint: string;
  aiTitles: string[];
  aiMessages: string[];
}

const DEFAULT: RoleGuidance = {
  welcomeSubtitle: 'Set up your hospital compliance system with guided steps',
  nextHint: 'Complete hospital profile, frameworks, and assessment to go operational.',
  aiTitles: [
    'Welcome',
    'Hospital Profile Setup',
    'Framework Selection',
    'AI Compliance Assessment',
    'Team Collaboration',
    'Review & submit',
  ],
  aiMessages: [
    "I'll guide you through setting up your hospital's compliance system. Let's get started!",
    'Tell us about your hospital so we can tailor frameworks and structure.',
    'Select the compliance frameworks that match your accreditation goals.',
    'Answer a few questions so we can estimate maturity and priorities.',
    'Invite colleagues who will help run compliance — or skip and invite later.',
    'Confirm your answers and submit to finish onboarding.',
  ],
};

const BY_ROLE: Record<string, Partial<RoleGuidance>> = {
  ORG_ADMIN: {
    welcomeSubtitle: 'As organization administrator, you own provisioning completion and team access.',
    nextHint: 'Finish hospital setup, enroll frameworks, then invite your quality leads.',
  },
  HOSPITAL_ADMIN: {
    welcomeSubtitle: 'Configure this hospital’s profile and compliance baselines.',
    nextHint: 'Confirm hospital details and frameworks for this site.',
  },
  QUALITY_HEAD: {
    welcomeSubtitle: 'Focus on frameworks and assessment readiness for quality leadership.',
    nextHint: 'Review framework selection and complete the compliance assessment.',
    aiMessages: [
      ...DEFAULT.aiMessages.slice(0, 3),
      'Your assessment answers drive quality priorities — be precise where possible.',
      'Invite department owners who will own evidence and CAPA.',
      DEFAULT.aiMessages[5],
    ],
  },
  QUALITY_MANAGER: {
    welcomeSubtitle: 'Focus on frameworks and assessment readiness.',
    nextHint: 'Complete assessment and confirm framework coverage.',
  },
  COMPLIANCE_OFFICER: {
    welcomeSubtitle: 'Prioritize framework enrollment and assessment accuracy.',
    nextHint: 'Validate frameworks and assessment responses.',
  },
  DEPARTMENT_HEAD: {
    welcomeSubtitle: 'Your departments will appear once admin setup finishes.',
    nextHint: 'Admin must complete hospital and framework steps; you can assist on assessment.',
  },
  AUDITOR: {
    welcomeSubtitle: 'Review assessment and readiness; setup gates are owned by administrators.',
    nextHint: 'Focus on assessment review once frameworks are selected.',
  },
};

export function resolveRoleGuidance(role: string | null | undefined): RoleGuidance {
  const key = (role || 'ORG_ADMIN').toUpperCase().replace(/\s+/g, '_');
  const aliases: Record<string, string> = {
    QUALITYHEAD: 'QUALITY_HEAD',
    QUALITY_MGR: 'QUALITY_MANAGER',
    DEPT_HEAD: 'DEPARTMENT_HEAD',
    DEPARTMENTHEAD: 'DEPARTMENT_HEAD',
  };
  const normalized = aliases[key] || key;
  const override = BY_ROLE[normalized] || {};
  return { ...DEFAULT, ...override, aiTitles: override.aiTitles || DEFAULT.aiTitles, aiMessages: override.aiMessages || DEFAULT.aiMessages };
}
