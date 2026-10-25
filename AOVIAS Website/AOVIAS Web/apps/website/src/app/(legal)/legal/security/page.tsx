import {
  LegalDocumentRoute,
  buildLegalDocumentMetadata,
} from '@/features/legal/create-legal-route';

export const metadata = buildLegalDocumentMetadata('security');

export default function SecurityOverviewPage() {
  return <LegalDocumentRoute documentId="security" />;
}
