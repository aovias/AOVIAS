import type { ContentImage } from '@/components/content';
import type { MediaSlot } from '@/components/product';

import { ArchitectureDiagram } from './architecture-diagram';
import { CommandMissionControl } from './command-mission-control';
import type { KnowledgeCoverTone } from './knowledge-cover';
import { PlatformServicesConsole } from './platform-services-console';
import {
  AnalyticsBoard,
  DashboardConsole,
  DeploymentPipeline,
  MonitoringConsole,
} from './product-consoles';
import { ProductFrame } from './product-frame';
import { StudioAuthoringWorkspace } from './studio-authoring-workspace';
import { WorkflowPanel } from './workflow-panel';

export type ProductMediaKind =
  | 'frame'
  | 'architecture'
  | 'workflow'
  | 'command'
  | 'studio'
  | 'platform'
  | 'mission-control'
  | 'authoring'
  | 'dashboard'
  | 'analytics'
  | 'monitoring'
  | 'deployment';

function inferProductMediaKind(label: string): ProductMediaKind {
  const lower = label.toLowerCase();
  if (lower.includes('architecture') || lower.includes('diagram')) {
    return 'architecture';
  }
  if (
    lower.includes('analytics') ||
    lower.includes('insight') ||
    lower.includes('metric')
  ) {
    return 'analytics';
  }
  if (lower.includes('monitor') || lower.includes('observab')) {
    return 'monitoring';
  }
  if (lower.includes('deploy') || lower.includes('release')) {
    return 'deployment';
  }
  if (
    lower.includes('mission control') ||
    lower.includes('incident') ||
    lower.includes('operations dashboard')
  ) {
    return 'mission-control';
  }
  if (
    lower.includes('authoring') ||
    lower.includes('publishing') ||
    lower.includes('editorial') ||
    lower.includes('library')
  ) {
    return 'authoring';
  }
  if (
    lower.includes('platform service') ||
    lower.includes('services console') ||
    (lower.includes('platform') && lower.includes('console'))
  ) {
    return 'platform';
  }
  if (
    lower.includes('dashboard') ||
    lower.includes('platform service') ||
    lower.includes('services console')
  ) {
    return 'dashboard';
  }
  if (
    lower.includes('workflow') ||
    lower.includes('capability') ||
    lower.includes('visibility') ||
    lower.includes('control') ||
    lower.includes('reliability') ||
    lower.includes('handoff') ||
    lower.includes('standards') ||
    lower.includes('design') ||
    lower.includes('lifecycle') ||
    lower.includes('intelligence')
  ) {
    return 'workflow';
  }
  if (lower.includes('command')) return 'command';
  if (lower.includes('studio')) return 'studio';
  if (lower.includes('platform')) return 'platform';
  return 'frame';
}

function commandWorkflowSteps(label: string): readonly string[] {
  const lower = label.toLowerCase();
  if (
    lower.includes('detect') ||
    lower.includes('investigate') ||
    lower.includes('operational') ||
    lower.includes('workflow')
  ) {
    return ['Detect', 'Investigate', 'Approve', 'Deploy', 'Observe'];
  }
  if (lower.includes('visibility')) {
    return ['Detect', 'Investigate', 'Prioritize', 'Share'];
  }
  if (lower.includes('control')) {
    return ['Investigate', 'Approve', 'Deploy', 'Observe'];
  }
  if (lower.includes('reliability') || lower.includes('intelligence')) {
    return ['Detect', 'Contain', 'Recover', 'Learn'];
  }
  return ['Detect', 'Investigate', 'Approve', 'Deploy', 'Observe'];
}

function studioWorkflowSteps(label: string): readonly string[] {
  const lower = label.toLowerCase();
  if (
    lower.includes('publish') ||
    lower.includes('lifecycle') ||
    lower.includes('delivery') ||
    lower.includes('workflow')
  ) {
    return ['Model', 'Review', 'Publish', 'Version', 'Release'];
  }
  if (lower.includes('design')) {
    return ['Model', 'Review', 'Align', 'Publish'];
  }
  if (lower.includes('standards')) {
    return ['Define', 'Apply', 'Verify', 'Govern'];
  }
  if (lower.includes('handoff') || lower.includes('collaboration')) {
    return ['Package', 'Transfer', 'Confirm', 'Support'];
  }
  return ['Model', 'Review', 'Publish', 'Version', 'Release'];
}

function platformWorkflowSteps(): readonly string[] {
  return ['Identity', 'Policies', 'Services', 'Deployments', 'Observability'];
}

/**
 * Brand illustration MediaSlot for product surfaces.
 */
export function productMediaSlot(
  label: string,
  kind?: ProductMediaKind,
): MediaSlot {
  const resolved = kind ?? inferProductMediaKind(label);
  const lower = label.toLowerCase();
  const isCommand = lower.includes('command');
  const isStudio = lower.includes('studio');
  const isPlatform = lower.includes('platform');

  if (resolved === 'architecture') {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <ArchitectureDiagram title={label} className="border-0" />,
    };
  }

  if (resolved === 'workflow') {
    const steps = isStudio
      ? studioWorkflowSteps(label)
      : isCommand
        ? commandWorkflowSteps(label)
        : isPlatform
          ? platformWorkflowSteps()
          : undefined;
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: (
        <WorkflowPanel
          title={label}
          className="border-0"
          {...(steps ? { steps } : {})}
        />
      ),
    };
  }

  if (resolved === 'mission-control') {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <CommandMissionControl title={label} className="border-0" />,
    };
  }

  if (resolved === 'authoring') {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <StudioAuthoringWorkspace title={label} className="border-0" />,
    };
  }

  if (resolved === 'platform' || (resolved === 'frame' && isPlatform)) {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <PlatformServicesConsole title={label} className="border-0" />,
    };
  }

  if (resolved === 'command' || (resolved === 'frame' && isCommand)) {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <CommandMissionControl title={label} className="border-0" />,
    };
  }

  if (resolved === 'studio' || (resolved === 'frame' && isStudio)) {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <StudioAuthoringWorkspace title={label} className="border-0" />,
    };
  }

  if (resolved === 'dashboard') {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <DashboardConsole title={label} className="border-0" />,
    };
  }

  if (resolved === 'analytics') {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <AnalyticsBoard title={label} className="border-0" />,
    };
  }

  if (resolved === 'monitoring') {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <MonitoringConsole title={label} className="border-0" />,
    };
  }

  if (resolved === 'deployment') {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <DeploymentPipeline title={label} className="border-0" />,
    };
  }

  // Prefer high-fidelity consoles over abstract ProductFrame when possible
  if (isCommand) {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <CommandMissionControl title={label} className="border-0" />,
    };
  }
  if (isStudio) {
    return {
      kind: 'custom',
      alt: label,
      aspect: 'video',
      node: <StudioAuthoringWorkspace title={label} className="border-0" />,
    };
  }

  return {
    kind: 'custom',
    alt: label,
    aspect: 'video',
    node: <ProductFrame title={label} variant="platform" className="border-0" />,
  };
}

/**
 * Knowledge / home resource cover as ContentImage.
 */
export function knowledgeCoverImage(
  label: string,
  tone: KnowledgeCoverTone = 'default',
  aspect: 'video' | 'wide' | 'square' = 'video',
): ContentImage {
  return {
    kind: 'cover',
    label,
    tone,
    aspect,
  };
}
