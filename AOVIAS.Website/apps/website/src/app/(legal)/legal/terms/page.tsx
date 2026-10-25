import {
  LegalDocumentRoute,
  buildLegalDocumentMetadata,
} from '@/features/legal/create-legal-route';

export const metadata = buildLegalDocumentMetadata('terms');

export default function TermsOfServicePage() {
  return <LegalDocumentRoute documentId="terms" />;
}
