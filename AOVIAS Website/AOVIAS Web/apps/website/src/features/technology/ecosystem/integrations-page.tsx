import {
  integrationsEditorialContent,
  TechnologyEditorialTemplate,
} from '@/features/technology/editorial';

/** Integrations hub — Enterprise Technology Library editorial (Phase V5). */
export function IntegrationsPage() {
  return (
    <TechnologyEditorialTemplate content={integrationsEditorialContent} />
  );
}
