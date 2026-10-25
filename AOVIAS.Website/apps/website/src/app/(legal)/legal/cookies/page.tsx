import {
  LegalDocumentRoute,
  buildLegalDocumentMetadata,
} from '@/features/legal/create-legal-route';

export const metadata = buildLegalDocumentMetadata('cookies');

export default function CookiePolicyPage() {
  return <LegalDocumentRoute documentId="cookies" />;
}
