export {
  getAllLegalDocuments,
  getLegalDocument,
  getLegalHubCards,
  getRelatedLegalCards,
  legalDocuments,
  toLegalDocumentCard,
} from '@/features/legal/domain/documents';
export { LegalDocumentPage } from '@/features/legal/legal-document-page';
export { TrustHubPage } from '@/features/legal/trust-hub-page';
export { legalDocumentSeo, legalHubSeo } from '@/features/legal/seo';
export {
  buildTocFromBody,
  formatLegalDate,
  type LegalBodySection,
  type LegalDocument,
  type LegalDocumentCard,
  type LegalDocumentId,
  type LegalDocumentMeta,
} from '@/features/legal/types';
