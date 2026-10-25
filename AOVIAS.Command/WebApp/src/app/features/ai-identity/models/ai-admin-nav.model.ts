export const AI_ADMIN_BASE = '/app/settings/ai';

export interface AiAdminNavItem {
  label: string;
  route: string;
  segment: string;
  icon: string;
  enabled: boolean;
  comingSoon?: boolean;
}

export interface AiAdminCapabilityCard {
  label: string;
  description: string;
  route?: string;
  icon: string;
  comingSoon?: boolean;
}

export function getAiAdminNavItems(): AiAdminNavItem[] {
  return [
    {
      label: 'Dashboard',
      route: `${AI_ADMIN_BASE}/dashboard`,
      segment: 'dashboard',
      icon: 'dashboard',
      enabled: true,
    },
    {
      label: 'AI Identity',
      route: `${AI_ADMIN_BASE}/identity`,
      segment: 'identity',
      icon: 'badge',
      enabled: true,
    },
    {
      label: 'Models',
      route: `${AI_ADMIN_BASE}/models`,
      segment: 'models',
      icon: 'psychology',
      enabled: true,
    },
    {
      label: 'Providers',
      route: `${AI_ADMIN_BASE}/providers`,
      segment: 'providers',
      icon: 'cloud',
      enabled: true,
    },
    {
      label: 'Prompt Library',
      route: `${AI_ADMIN_BASE}/prompts`,
      segment: 'prompts',
      icon: 'description',
      enabled: true,
    },
    {
      label: 'Policies',
      route: `${AI_ADMIN_BASE}/policies`,
      segment: 'policies',
      icon: 'policy',
      enabled: true,
    },
    {
      label: 'Executions',
      route: `${AI_ADMIN_BASE}/executions`,
      segment: 'executions',
      icon: 'history',
      enabled: true,
    },
    {
      label: 'Usage & Analytics',
      route: `${AI_ADMIN_BASE}/analytics`,
      segment: 'analytics',
      icon: 'insights',
      enabled: true,
    },
    {
      label: 'Approvals',
      route: `${AI_ADMIN_BASE}/approvals`,
      segment: 'approvals',
      icon: 'task_alt',
      enabled: true,
    },
  ];
}

export function getAiAdminCapabilityCards(): AiAdminCapabilityCard[] {
  return [
    {
      label: 'AI Identity',
      description: 'Governed AI identity profiles bound to platform modules.',
      route: `${AI_ADMIN_BASE}/identity`,
      icon: 'badge',
    },
    {
      label: 'Models',
      description: 'Observed model inventory derived from execution telemetry.',
      route: `${AI_ADMIN_BASE}/models`,
      icon: 'psychology',
    },
    {
      label: 'Providers',
      description: 'Provider catalog and health inferred from platform usage.',
      route: `${AI_ADMIN_BASE}/providers`,
      icon: 'cloud',
    },
    {
      label: 'Prompt Library',
      description: 'Browse published prompts and governed prompt layers.',
      route: `${AI_ADMIN_BASE}/prompts`,
      icon: 'description',
    },
    {
      label: 'Governance Policies',
      description: 'Automation modes, confidence thresholds, and risk levels.',
      route: `${AI_ADMIN_BASE}/policies`,
      icon: 'policy',
    },
    {
      label: 'Executions',
      description: 'Audit trail of AI executions and correlation identifiers.',
      route: `${AI_ADMIN_BASE}/executions`,
      icon: 'history',
    },
    {
      label: 'Usage & Analytics',
      description: 'Usage, latency, and acceptance metrics for governed AI.',
      route: `${AI_ADMIN_BASE}/analytics`,
      icon: 'insights',
    },
    {
      label: 'Approval Queue',
      description: 'Review and grant pending automation approvals.',
      route: `${AI_ADMIN_BASE}/approvals`,
      icon: 'task_alt',
    },
  ];
}

export function getAiAdminFuturePlaceholders(): AiAdminCapabilityCard[] {
  return [
    {
      label: 'AI Marketplace',
      description: 'Discover and install curated AI capabilities.',
      icon: 'storefront',
      comingSoon: true,
    },
    {
      label: 'Custom Skills',
      description: 'Organization-specific AI skills and playbooks.',
      icon: 'extension',
      comingSoon: true,
    },
    {
      label: 'MCP Servers',
      description: 'Model Context Protocol server registry and bindings.',
      icon: 'dns',
      comingSoon: true,
    },
    {
      label: 'Agent Registry',
      description: 'Catalog and govern autonomous agents.',
      icon: 'smart_toy',
      comingSoon: true,
    },
    {
      label: 'Knowledge Bases',
      description: 'Connect curated knowledge for retrieval workflows.',
      icon: 'menu_book',
      comingSoon: true,
    },
    {
      label: 'Fine-Tuning',
      description: 'Managed fine-tuning jobs and evaluation sets.',
      icon: 'tune',
      comingSoon: true,
    },
    {
      label: 'Semantic Memory',
      description: 'Long-term semantic memory for assistants and agents.',
      icon: 'memory',
      comingSoon: true,
    },
    {
      label: 'Cost Management',
      description: 'Budgets, quotas, and spend controls across providers.',
      icon: 'payments',
      comingSoon: true,
    },
  ];
}

/** Known provider catalog for Administration display (no dedicated registry API). */
export const AI_PROVIDER_CATALOG: Array<{
  key: string;
  name: string;
  aliases: string[];
}> = [
  { key: 'azureopenai', name: 'Azure OpenAI', aliases: ['azure', 'azureopenai', 'azure-openai'] },
  { key: 'openai', name: 'OpenAI', aliases: ['openai'] },
  { key: 'sarvam', name: 'Sarvam', aliases: ['sarvam'] },
  { key: 'ollama', name: 'Ollama', aliases: ['ollama'] },
  { key: 'anthropic', name: 'Anthropic', aliases: ['anthropic', 'claude'] },
  { key: 'google', name: 'Google', aliases: ['google', 'gemini', 'vertex'] },
];

export function resolveAiAdminBreadcrumb(url: string): string {
  const path = url.split(/[?#]/)[0].replace(/\/+$/, '');
  if (path === AI_ADMIN_BASE) {
    return 'Dashboard';
  }

  const items = getAiAdminNavItems().slice().sort((a, b) => b.route.length - a.route.length);

  for (const item of items) {
    if (path === item.route) {
      return item.label;
    }
    if (item.segment && path.startsWith(`${AI_ADMIN_BASE}/${item.segment}/`)) {
      return item.label;
    }
  }

  if (path.startsWith(`${AI_ADMIN_BASE}/permissions`)) {
    return 'Permissions';
  }

  return 'AI Administration';
}

export function categorizePromptLayer(layerType: string): string {
  const value = (layerType ?? '').toLowerCase();
  if (value.includes('compliance')) return 'Compliance';
  if (value.includes('audit')) return 'Audit';
  if (value.includes('evidence')) return 'Evidence';
  if (value.includes('report')) return 'Reports';
  if (value.includes('support') || value.includes('assist')) return 'Support';
  return 'General';
}
