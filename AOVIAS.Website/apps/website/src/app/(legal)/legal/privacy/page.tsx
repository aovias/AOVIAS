import {
  LegalDocumentRoute,
  buildLegalDocumentMetadata,
} from '@/features/legal/create-legal-route';

export const metadata = buildLegalDocumentMetadata('privacy');

export default function PrivacyPolicyPage() {
  return <LegalDocumentRoute documentId="privacy" />;
}
