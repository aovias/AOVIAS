export type ControlledDocumentType =
  | 'policy'
  | 'sop'
  | 'manual'
  | 'guideline'
  | 'protocol'
  | 'procedure'
  | 'checklist'
  | 'form'
  | 'template'
  | 'committeeDocument'
  | 'externalReference';

export type ControlledDocumentStatus =
  | 'draft'
  | 'underReview'
  | 'approved'
  | 'published'
  | 'effective'
  | 'reviewDue'
  | 'superseded'
  | 'archived';

export type DocumentApprovalStatus = 'pending' | 'approved' | 'rejected' | 'skipped';

export type DocumentAcknowledgementAssigneeType = 'department' | 'role' | 'user';

export const DOCUMENT_TYPE_LABELS: Record<ControlledDocumentType, string> = {
  policy: 'Policy',
  sop: 'SOP',
  manual: 'Manual',
  guideline: 'Guideline',
  protocol: 'Protocol',
  procedure: 'Procedure',
  checklist: 'Checklist',
  form: 'Form',
  template: 'Template',
  committeeDocument: 'Committee Document',
  externalReference: 'External Reference',
};

export const DOCUMENT_STATUS_LABELS: Record<ControlledDocumentStatus, string> = {
  draft: 'Draft',
  underReview: 'Under Review',
  approved: 'Approved',
  published: 'Published',
  effective: 'Effective',
  reviewDue: 'Review Due',
  superseded: 'Superseded',
  archived: 'Archived',
};

export const TYPE_FILTER_MAP: Record<string, ControlledDocumentType | undefined> = {
  policies: 'policy',
  sops: 'sop',
  manuals: 'manual',
  forms: 'form',
};
