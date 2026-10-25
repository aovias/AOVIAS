/**
 * Pure FormData builders for Gateway evidence multipart APIs.
 * @see ComplaincePlatform/docs/backend/gateway-evidence-api-specification.md (§5.3, §5.5, §5.9)
 * @see docs/backend/gateway-evidence-api-reference.md
 */

export interface BuildSingleEvidenceFormDataParams {
  file: File;
  evidenceType: string;
  title?: string;
  /** Sent as JSON string if an object is passed. */
  metadata?: Record<string, unknown> | string;
}

/**
 * `POST …/objective-elements/{oeId}/evidence` — fields: `file`, `evidenceType`, optional `name`, `title`, `metadata`.
 * Backend: `name` takes precedence over `title` when both are sent; we set `name` from display title or file name.
 *
 * Use **`append('file', file, file.name)`** so the part has an explicit filename in `Content-Disposition` (Gateway / ASP.NET model binding).
 * Do not set `Content-Type` on the multipart client; the runtime adds `multipart/form-data` + boundary.
 */
export function buildSingleEvidenceFormData(params: BuildSingleEvidenceFormDataParams): FormData {
  const evidenceType = (params.evidenceType ?? '').trim();
  if (!evidenceType) {
    throw new Error('evidenceType must be a non-empty string (Gateway multipart contract).');
  }
  const fd = new FormData();
  fd.append('file', params.file, params.file.name);
  /** Display name — §4.8 in journey spec; some gateways bind `name` alongside `title`. */
  const displayName = (params.title ?? '').trim() || params.file.name;
  fd.append('name', displayName);
  fd.append('evidenceType', evidenceType);
  if (params.title !== undefined && params.title !== '') {
    fd.append('title', params.title);
  }
  if (params.metadata !== undefined) {
    const meta =
      typeof params.metadata === 'string' ? params.metadata : JSON.stringify(params.metadata);
    fd.append('metadata', meta);
  }
  return fd;
}

export interface BuildBulkEvidenceFormDataParams {
  files: File[];
  evidenceType: string;
  /** Sent as JSON string if an object is passed. */
  sharedMetadata?: Record<string, unknown> | string;
}

/**
 * `POST …/objective-elements/{oeId}/evidence/bulk`
 *
 * Appends each file with field name **`files`** (repeated parts). Verify `files` vs `files[]`
 * against deployed Gateway OpenAPI if uploads fail with 415/400.
 */
export function buildBulkEvidenceFormData(params: BuildBulkEvidenceFormDataParams): FormData {
  const evidenceType = (params.evidenceType ?? '').trim();
  if (!evidenceType) {
    throw new Error('evidenceType must be a non-empty string (Gateway multipart contract).');
  }
  const fd = new FormData();
  for (const file of params.files) {
    fd.append('files', file, file.name);
  }
  fd.append('evidenceType', evidenceType);
  if (params.sharedMetadata !== undefined) {
    const meta =
      typeof params.sharedMetadata === 'string'
        ? params.sharedMetadata
        : JSON.stringify(params.sharedMetadata);
    fd.append('sharedMetadata', meta);
  }
  return fd;
}

export interface BuildEvidenceVersionFormDataParams {
  file: File;
  /** Optional version notes (Gateway §5.9). */
  notes?: string;
}

/**
 * `POST …/objective-elements/{oeId}/evidence/{evidenceId}/versions` — fields: `file` (required), `notes` (optional).
 */
export function buildEvidenceVersionFormData(params: BuildEvidenceVersionFormDataParams): FormData {
  const fd = new FormData();
  fd.append('file', params.file, params.file.name);
  if (params.notes !== undefined && params.notes !== '') {
    fd.append('notes', params.notes);
  }
  return fd;
}
