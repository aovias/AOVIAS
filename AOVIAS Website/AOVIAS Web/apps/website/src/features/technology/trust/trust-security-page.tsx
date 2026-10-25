import {
  securityEditorialContent,
  TechnologyEditorialTemplate,
} from '@/features/technology/editorial';

/**
 * Security hub (route: /technology/trust) — Enterprise Technology Library editorial (Phase V5).
 */
export function TrustSecurityPage() {
  return <TechnologyEditorialTemplate content={securityEditorialContent} />;
}
