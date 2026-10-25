import { JsonLdScript } from '@/components/seo';
import {
  LegalDocumentPage,
  getLegalDocument,
  legalDocumentSeo,
  type LegalDocumentId,
} from '@/features/legal';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

export function buildLegalDocumentMetadata(documentId: LegalDocumentId) {
  const seo = legalDocumentSeo[documentId];
  return createPageMetadata({
    title: seo.title,
    description: seo.description,
    path: seo.path,
    type: 'legal',
    keywords: seo.keywords,
  });
}

export type LegalDocumentRouteProps = {
  documentId: LegalDocumentId;
};

/**
 * Thin legal document route shell — JSON-LD + shared document page.
 */
export function LegalDocumentRoute({ documentId }: LegalDocumentRouteProps) {
  const seo = legalDocumentSeo[documentId];
  const doc = getLegalDocument(documentId);

  return (
    <>
      <JsonLdScript
        id={`legal-${documentId}-webpage`}
        data={buildPageSchemas({
          title: seo.title,
          description: seo.description,
          path: seo.path,
          includeOrganization: true,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Trust & Governance', href: '/legal' },
            { label: doc.title, href: seo.path, current: true },
          ],
        })}
      />
      <LegalDocumentPage documentId={documentId} />
    </>
  );
}
