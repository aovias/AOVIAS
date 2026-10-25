import {
  engineeringEditorialContent,
  TechnologyEditorialTemplate,
} from '@/features/technology/editorial';

/** Engineering hub — Enterprise Technology Library editorial (Phase V5). */
export function EngineeringHubPage() {
  return <TechnologyEditorialTemplate content={engineeringEditorialContent} />;
}
