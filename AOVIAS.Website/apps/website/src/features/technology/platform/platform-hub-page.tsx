import {
  platformEditorialContent,
  TechnologyEditorialTemplate,
} from '@/features/technology/editorial';

/** Platform hub — Enterprise Technology Library editorial (Phase V5). */
export function PlatformHubPage() {
  return <TechnologyEditorialTemplate content={platformEditorialContent} />;
}
