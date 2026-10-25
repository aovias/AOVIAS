import {
  LegalDocumentRoute,
  buildLegalDocumentMetadata,
} from '@/features/legal/create-legal-route';

export const metadata = buildLegalDocumentMetadata('accessibility');

export default function AccessibilityStatementPage() {
  return <LegalDocumentRoute documentId="accessibility" />;
}
