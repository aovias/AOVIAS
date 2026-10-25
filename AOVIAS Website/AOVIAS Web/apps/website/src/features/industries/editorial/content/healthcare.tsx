import {
  BarChart3,
  CalendarCheck,
  Download,
  Lock,
  PlayCircle,
  ShieldCheck,
  Users,
  UsersRound,
} from 'lucide-react';

import { HealthcareCommandHero } from '@/components/brand';
import type { IndustryEditorialContent } from '@/features/industries/editorial/types';

/** Healthcare — AOVIAS Command hospital operations landing (conversion refinement). */
export const healthcareEditorialContent = {
  slug: 'healthcare',
  name: 'Healthcare',
  atmosphere: 'aurora',
  domainVariant: 'healthcare',
  environmentVariant: 'operations',
  seo: {
    title: 'AOVIAS Command for Hospitals — NABH Compliance & Operational Governance',
    description:
      'AOVIAS Command helps hospitals stay continuously audit-ready with centralized compliance, corrective-action ownership, and executive visibility — without replacing your HMS.',
    keywords: [
      'AOVIAS Command healthcare',
      'NABH compliance software',
      'hospital operational platform',
      'healthcare governance software',
      'hospital audit readiness',
      'hospital compliance management',
      'healthcare quality management system',
    ],
  },
  hero: {
    eyebrow: 'AOVIAS Command for Hospitals',
    title:
      'Simplify NABH Compliance, Operational Governance and Hospital-wide Accountability',
    description:
      'Built for hospitals that want to stay continuously audit-ready while reducing administrative effort through one integrated operational platform — not another Hospital Management System.',
    highlights: [
      {
        id: 'audit-readiness',
        label: 'Continuous Audit Readiness',
        description: 'Stay always inspection-ready',
        icon: ShieldCheck,
      },
      {
        id: 'accountability',
        label: 'Operational Accountability',
        description: 'Clear ownership and task accountability',
        icon: Users,
      },
      {
        id: 'visibility',
        label: 'Executive Visibility',
        description: 'Real-time insights across departments',
        icon: BarChart3,
      },
      {
        id: 'security',
        label: 'Enterprise Grade Security',
        description: 'Security, privacy and governance by design',
        icon: Lock,
      },
    ],
    primaryCta: {
      label: 'Request Demo',
      href: '/company/contact#form-demo',
    },
    secondaryCta: {
      label: 'Watch Demo',
      href: '#demo',
      icon: PlayCircle,
    },
    tertiaryCta: {
      label: 'Download Brochure',
      href: '#brochure',
      icon: Download,
    },
    actionLinks: [
      {
        id: 'consultation',
        label: 'Book Consultation',
        href: '/company/contact#form-demo',
        icon: CalendarCheck,
      },
      {
        id: 'walkthrough',
        label: 'Schedule Walkthrough',
        href: '/company/contact#form-demo',
        icon: UsersRound,
      },
    ],
    illustrationCaption: 'Compliance · governance · accountability',
  },
  heroIllustration: (
    <HealthcareCommandHero className="xl:min-h-[20rem] 2xl:min-h-[24rem]" />
  ),
  demo: {
    eyebrow: 'Product demonstration',
    title: 'See AOVIAS Command in Action',
    description:
      'Reduce uncertainty before you book a meeting. Watch real product demonstrations showing how hospitals manage governance, evidence, and operational readiness using AOVIAS Command.',
    videos: [
      {
        id: 'command-light',
        title: 'Light Theme Demonstration',
        description:
          'Experience AOVIAS Command using the Light workspace, designed for clarity and operational workflows.',
        youtubeId: 'pK7jWR3V3XU',
        duration: '3 min',
        theme: 'light',
        badge: 'NEW',
      },
      {
        id: 'command-dark',
        title: 'Dark Theme Demonstration',
        description:
          'Explore the Dark workspace optimized for continuous monitoring and executive operations.',
        youtubeId: 'OCr79KWmIJM',
        duration: '3 min',
        theme: 'dark',
      },
    ],
  },
  reality: {
    eyebrow: 'Hospital operating reality',
    title: 'Accreditation pressure meets day-to-day operations',
    description:
      'Quality teams, department heads, and hospital leadership share one obligation: remain continuously ready for review while care delivery continues. Spreadsheets and email cannot carry that responsibility alone.',
    pillars: [
      'NABH readiness',
      'Clinical operations',
      'Quality governance',
      'Multi-department accountability',
    ],
  },
  challenges: {
    eyebrow: 'Operational challenges',
    title: 'Where traditional compliance management breaks down',
    description:
      'Familiar hospital pressures that turn audit preparation into a recurring institutional crisis.',
    items: [
      {
        id: 'fragmented',
        title: 'Fragmented evidence',
        description:
          'Policies, checklists, and proof live across folders, email threads, and paper files — ownership is unclear when auditors ask.',
      },
      {
        id: 'ownership',
        title: 'Unclear corrective ownership',
        description:
          'Actions are assigned verbally or by email, then stall without escalation paths leadership can trust.',
      },
      {
        id: 'nabh',
        title: 'NABH cycle intensity',
        description:
          'Preparation surges before assessment windows, then discipline fades — continuous readiness never becomes routine.',
      },
      {
        id: 'visibility',
        title: 'Limited executive visibility',
        description:
          'Directors lack a single view of department performance, open findings, and governance risk across the hospital.',
      },
      {
        id: 'burden',
        title: 'Administrative burden',
        description:
          'Quality and operations staff reconcile the same information in multiple tools instead of acting on it.',
      },
      {
        id: 'systems',
        title: 'HMS is not enough',
        description:
          'Clinical and billing systems run the hospital — they were never designed to govern accreditation workflows end to end.',
      },
    ],
  },
  productExperience: {
    eyebrow: 'Product experience',
    title: 'How Hospital Teams Work in AOVIAS Command',
    description:
      'AOVIAS Command provides dedicated operational workspaces for leadership, quality teams, and departments—bringing governance, evidence, accountability, and executive visibility into one institutional platform.',
    items: [
      {
        id: 'executive',
        category: 'Leadership',
        title: 'Executive Dashboard',
        description:
          'Hospital leadership gains a live operational view of readiness, department performance, corrective actions, and governance indicators. The dashboard concentrates institutional signal—without dashboard theater.',
        capabilities: [
          'Institution-wide visibility',
          'Readiness monitoring',
          'Governance indicators',
        ],
        imageSrc: '/product/healthcare/executive-dashboard.svg',
        imageAlt:
          'AOVIAS Command executive dashboard showing compliance, tasks, CAPA, and department performance',
        frame: 'desktop',
      },
      {
        id: 'compliance',
        category: 'Quality',
        title: 'Compliance Workspace',
        description:
          'Quality teams manage standards, objective elements, assessments, and reviews in one governed workspace—with clear ownership for every control and continuous inspection readiness.',
        capabilities: [
          'Standards and objective elements',
          'Assessments and reviews',
          'Control ownership',
        ],
        imageSrc: '/product/healthcare/compliance-workspace.svg',
        imageAlt:
          'AOVIAS Command compliance workspace with standards library and objective elements',
        frame: 'desktop',
      },
      {
        id: 'evidence',
        category: 'Governance',
        title: 'Evidence Repository',
        description:
          'Evidence is indexed, versioned, and linked to standards so audit trails remain attributable. Search and retrieval support continuous readiness—not last-minute document hunts.',
        capabilities: [
          'Version history and linked standards',
          'Searchable institutional evidence',
          'Audit traceability',
        ],
        imageSrc: '/product/healthcare/evidence-repository.svg',
        imageAlt:
          'AOVIAS Command evidence repository with indexed files linked to standards',
        frame: 'desktop',
      },
      {
        id: 'capa',
        category: 'Accountability',
        title: 'Corrective Action Tracking',
        description:
          'Every finding becomes an owned corrective action with deadlines, progress, and escalation. Leadership sees what is open, due, and overdue—before it becomes an audit surprise.',
        capabilities: [
          'CAPA assignments and deadlines',
          'Escalations for overdue work',
          'Visible progress to closure',
        ],
        imageSrc: '/product/healthcare/corrective-actions.svg',
        imageAlt:
          'AOVIAS Command corrective action tracking with owners, due dates, and status',
        frame: 'desktop',
      },
      {
        id: 'dept',
        category: 'Departments',
        title: 'Department Performance',
        description:
          'Departments see pending work, ownership, and readiness trends without losing hospital-wide coherence. Accountability stays local; governance stays institutional.',
        capabilities: [
          'Department accountability',
          'Pending work and ownership',
          'Readiness trends',
        ],
        imageSrc: '/product/healthcare/department-performance.svg',
        imageAlt:
          'AOVIAS Command department performance cards for nursing, ICU, laboratory, and more',
        frame: 'desktop',
      },
      {
        id: 'mobile',
        category: 'Field operations',
        title: 'Mobile Experience',
        description:
          'On-floor teams capture evidence, complete assigned tasks, review policies, and progress corrective actions from a mobile workspace designed for hospital operations—not consumer apps.',
        capabilities: [
          'Evidence capture and approvals',
          'Assigned tasks and CAPA follow-through',
          'Field-ready operational workflows',
        ],
        imageSrc: '/product/healthcare/mobile-experience.svg',
        imageAlt:
          'AOVIAS Command mobile My Tasks experience for evidence upload and approvals',
        frame: 'mobile',
      },
    ],
  },
  outcomes: {
    eyebrow: 'Institutional outcomes',
    title: 'Operational improvements hospitals can govern',
    description:
      'Measurable in institutional practice — without inflated marketing statistics.',
    items: [
      {
        id: 'centralized',
        title: 'Centralized compliance management',
        description:
          'One governed workspace for standards, assessments, and evidence across departments.',
      },
      {
        id: 'capa-own',
        title: 'Clear ownership of corrective actions',
        description:
          'Every finding has an accountable owner, timeline, and visible status for leadership.',
      },
      {
        id: 'audit',
        title: 'Continuous audit readiness',
        description:
          'Readiness becomes routine operations — not a last-minute scramble before assessment.',
      },
      {
        id: 'exec',
        title: 'Executive visibility across departments',
        description:
          'Directors see risk, progress, and bottlenecks without chasing status by email.',
      },
      {
        id: 'workflows',
        title: 'Standardized governance workflows',
        description:
          'Repeatable institutional processes replace ad-hoc follow-ups and paper trails.',
      },
    ],
  },
  comparison: {
    eyebrow: 'Why hospitals change',
    title: 'Why Hospitals Move Beyond Traditional Compliance Management',
    description:
      'A disciplined comparison of how work actually gets done — not a marketing scorecard.',
    traditionalLabel: 'Traditional process',
    modernLabel: 'AOVIAS Command',
    traditional: [
      'Spreadsheets',
      'Email follow-ups',
      'Paper evidence',
      'Manual reminders',
      'Audit preparation surges',
    ],
    modern: [
      'Centralized platform',
      'Workflow automation',
      'Digital evidence repository',
      'Task ownership',
      'Continuous readiness',
    ],
  },
  workflow: {
    eyebrow: 'How AOVIAS Command helps',
    title: 'Standards → Ownership → Evidence → Review → Improvement',
    description:
      'A hospital operating rhythm for quality and governance — executable every day, inspectable at assessment.',
    steps: [
      'Standards & controls',
      'Department ownership',
      'Evidence capture',
      'Review & escalation',
      'Continuous improvement',
    ],
  },
  portfolio: {
    eyebrow: 'Solution portfolio',
    title: 'Command leads hospital operations — Studio and Platform support the institution',
    description:
      'AOVIAS Command is the operational surface for hospital governance. Studio and Platform strengthen standards and shared enterprise foundations.',
    items: [
      {
        id: 'command',
        name: 'AOVIAS Command',
        role: 'Operational command center for hospital compliance, accountability, and continuous audit readiness.',
        href: '/products/command',
      },
      {
        id: 'studio',
        name: 'AOVIAS Studio',
        role: 'Author and govern operational standards so policies stay coherent across facilities and teams.',
        href: '/products/studio',
      },
      {
        id: 'platform',
        name: 'AOVIAS Platform',
        role: 'Shared identity, security, and deployment foundations beneath every hospital capability.',
        href: '/products/platform',
      },
    ],
  },
  integrations: {
    eyebrow: 'Hospital systems',
    title: 'Works Alongside Your Existing Hospital Systems',
    description:
      'AOVIAS complements — it does not replace — your Hospital Management System or clinical stack. Command becomes the operational governance layer around the systems you already run.',
    hubLabel: 'AOVIAS Command',
    nodes: [
      { id: 'hms', label: 'Hospital Management System' },
      { id: 'abha', label: 'ABHA Ecosystem' },
      { id: 'hr', label: 'HR Systems' },
      { id: 'sso', label: 'Active Directory / SSO' },
      { id: 'email', label: 'Email' },
      { id: 'docs', label: 'Document Storage' },
      { id: 'erp', label: 'Future ERP Integrations' },
    ],
  },
  trustPillars: {
    eyebrow: 'Institutional trust',
    title: 'Built for hospitals that cannot afford careless systems',
    description:
      'Authentic design commitments — not fabricated testimonials or borrowed logos.',
    items: [
      {
        id: 'input',
        title: 'Hospital-informed design',
        description:
          'Built with input from hospital administrators and quality professionals who live accreditation cycles.',
      },
      {
        id: 'nabh',
        title: 'NABH-oriented workflows',
        description:
          'Designed around NABH-oriented operational workflows — evidence, ownership, and review discipline.',
      },
      {
        id: 'enterprise',
        title: 'Enterprise-first architecture',
        description:
          'Platform thinking for institutions: identity, security boundaries, and long-term operability.',
      },
      {
        id: 'security',
        title: 'Security and governance by design',
        description:
          'Controls and accountability are product fundamentals — not an afterthought module.',
      },
      {
        id: 'continuity',
        title: 'Institutional continuity',
        description:
          'Designed for continuity across shifts, departments, and leadership tenures.',
      },
    ],
  },
  journey: {
    eyebrow: 'Enterprise story',
    title: 'Governance, operationalized as a continuous cycle',
    description:
      'AOVIAS Command turns hospital governance into daily operating rhythm — from standards and ownership through evidence and review to executive visibility and continuous audit readiness.',
    model: 'operating-cycle',
    lead:
      'Compliance is not a binder refreshed before inspection. It is an operating model: standards set direction, departments own work, evidence accumulates in the flow of care, quality reviews findings, corrective actions close gaps, and leadership sees readiness in real time.',
    steps: [
      'Standards',
      'Department Ownership',
      'Daily Tasks',
      'Evidence Collection',
      'Quality Review',
      'Corrective Actions',
      'Executive Visibility',
      'Continuous Audit Readiness',
    ],
    cycleSteps: [
      {
        id: 'standards',
        label: 'Standards',
        summary:
          'Institutional policies and accreditation requirements become the governed baseline for every department.',
      },
      {
        id: 'ownership',
        label: 'Department Ownership',
        summary:
          'Accountability is assigned where work happens — with clear owners, not shared ambiguity.',
      },
      {
        id: 'tasks',
        label: 'Daily Tasks',
        summary:
          'Governance becomes executable work: checklists, reviews, and follow-ups in the operating day.',
      },
      {
        id: 'evidence',
        label: 'Evidence Collection',
        summary:
          'Proof is captured as work is done — versioned, linked, and ready when asked.',
      },
      {
        id: 'review',
        label: 'Quality Review',
        summary:
          'Quality teams assess posture continuously, not only in the weeks before an audit.',
      },
      {
        id: 'capa',
        label: 'Corrective Actions',
        summary:
          'Findings convert into owned CAPA with deadlines, escalations, and visible progress.',
      },
      {
        id: 'visibility',
        label: 'Executive Visibility',
        summary:
          'Leadership sees readiness, risk, and accountability across the institution in one view.',
      },
      {
        id: 'readiness',
        label: 'Continuous Audit Readiness',
        summary:
          'The cycle closes into sustained readiness — then informs the next round of standards.',
      },
    ],
  },
  narratives: {
    eyebrow: 'Institutional transformation',
    title: 'What Changes After AOVIAS Becomes Part of Your Hospital',
    description:
      'AOVIAS Command does not add another reporting layer. It changes how governance, evidence, and accountability operate every day — from fragmented effort to one continuous institutional platform.',
    model: 'transformation',
    items: [
      {
        id: 'governance',
        title: 'Continuous Institutional Governance',
        description:
          'Standards stop living in disconnected documents and become the operating baseline. Departments own their controls, quality oversees posture, and governance remains active between assessments — not only in the weeks before inspection.',
      },
      {
        id: 'evidence',
        title: 'Evidence Captured During Daily Work',
        description:
          'Evidence is produced as tasks are completed, not reconstructed under deadline pressure. Files, checklists, and reviews stay linked to standards with ownership and version history intact.',
      },
      {
        id: 'visibility',
        title: 'Leadership Without Operational Blind Spots',
        description:
          'Executives see readiness, open corrective actions, and departmental accountability in one governed view. Oversight becomes continuous institutional awareness rather than delayed, incomplete reporting.',
      },
    ],
    flowSteps: [
      { id: 'policies', label: 'Policies' },
      { id: 'tasks', label: 'Tasks' },
      { id: 'evidence', label: 'Evidence' },
      { id: 'review', label: 'Quality Review' },
      { id: 'capa', label: 'Corrective Actions' },
      { id: 'dashboard', label: 'Executive Dashboard' },
      { id: 'readiness', label: 'Continuous Audit Readiness' },
    ],
    outcomeStrip: [
      'Unified Governance',
      'Department Accountability',
      'Continuous Evidence',
      'Executive Visibility',
      'Audit Readiness',
    ],
  },
  brochure: {
    eyebrow: 'Executive briefing',
    title: 'Download the AOVIAS Command Healthcare Brochure',
    description:
      'A concise executive overview of how AOVIAS Command supports NABH-oriented governance, evidence, and hospital-wide accountability. Full PDF will be published here when available.',
    coverSrc: '/product/healthcare/brochure-cover.svg',
    coverAlt: 'AOVIAS Command healthcare executive brochure cover placeholder',
    downloadCta: {
      label: 'Download PDF',
      href: '#brochure',
    },
  },
  faq: {
    eyebrow: 'FAQ',
    title: 'Questions hospital leaders ask before evaluating AOVIAS',
    description:
      'Direct answers for procurement, quality, and IT stakeholders.',
    items: [
      {
        id: 'hms',
        question: 'Does AOVIAS replace our HMS?',
        answer:
          'No. AOVIAS Command complements your Hospital Management System. It provides the operational governance layer for compliance, evidence, and accountability — it is not a clinical or billing HMS replacement.',
        keywords: ['HMS', 'replace', 'hospital management'],
      },
      {
        id: 'nabh',
        question: 'How does AOVIAS support NABH?',
        answer:
          'Command structures standards, ownership, evidence, and corrective actions so hospitals can sustain NABH-oriented readiness as ongoing operations rather than periodic emergency preparation.',
        keywords: ['NABH', 'accreditation', 'compliance'],
      },
      {
        id: 'implementation',
        question: 'How is implementation managed?',
        answer:
          'Implementation is phased with your quality and operations stakeholders — focusing first on governance workflows, evidence practices, and department ownership, then expanding depth over time.',
        keywords: ['implementation', 'rollout', 'onboarding'],
      },
      {
        id: 'security',
        question: 'How is data secured?',
        answer:
          'AOVIAS is built enterprise-first: access control, auditability, and governance are foundational. Security controls and institutional deployment practices are discussed during evaluation.',
        keywords: ['security', 'data protection', 'access control'],
      },
      {
        id: 'multi',
        question: 'Can multiple hospitals be managed?',
        answer:
          'Yes. The platform is designed for institutional scale — including multi-facility groups that need shared standards with local departmental accountability.',
        keywords: ['multi-hospital', 'group', 'facilities'],
      },
      {
        id: 'integrate',
        question: 'Can it integrate with existing systems?',
        answer:
          'AOVIAS is designed to work alongside HMS, identity (SSO/AD), HR, email, document storage, ABHA-related ecosystems, and future ERP connections as your integration roadmap matures.',
        keywords: ['integration', 'SSO', 'ABHA', 'ERP'],
      },
    ],
  },
  midCtas: [
    {
      id: 'after-outcomes',
      title: 'Prefer a short overview first?',
      description: 'Watch the demo or speak with our team about institutional fit.',
      primaryCta: {
        label: 'Watch 3-Minute Demo',
        href: '#demo',
      },
      secondaryCta: {
        label: 'Request Demo',
        href: '/company/contact#form-demo',
      },
    },
    {
      id: 'before-faq',
      title: 'Take the executive overview with you',
      description: 'Download the brochure, then book a consultation when you are ready.',
      primaryCta: {
        label: 'Download Product Brochure',
        href: '#brochure',
      },
      secondaryCta: {
        label: 'Book Consultation',
        href: '/company/contact#form-demo',
      },
    },
  ],
  closing: {
    title: 'Stay continuously compliant, governed, and audit-ready.',
    description:
      'AOVIAS is not another HMS. It is the operational platform that helps your hospital sustain accountability — request a demonstration tailored to your institution.',
    primaryCta: {
      label: 'Request Demo',
      href: '/company/contact#form-demo',
    },
    secondaryCta: {
      label: 'Schedule Personalized Walkthrough',
      href: '/company/contact#form-demo',
    },
    tertiaryCta: {
      label: 'Explore AOVIAS Command',
      href: '/products/command',
    },
  },
} satisfies IndustryEditorialContent;
