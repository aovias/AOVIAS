/** Max file size per Gateway journey evidence spec (v1.3). */
export const EVIDENCE_MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024;

/** Max files per bulk multipart request (v1.3). */
export const EVIDENCE_BULK_MAX_FILES = 15;

/** Placeholder evidence taxonomy until OE/KB supplies a canonical type. */
export const OE_EVIDENCE_DEFAULT_EVIDENCE_TYPE = 'Document';

export type EvidenceUploadMode = 'single' | 'bulk';

export type EvidenceFilesValidationResult =
  | { ok: true }
  | { ok: false; message: string };

/**
 * Client-side validation for evidence selection before building FormData.
 * - **single**: exactly one file, each file ≤ max size.
 * - **bulk**: 1–15 files, each ≤ max size.
 */
export function validateEvidenceFilesForMode(
  files: File[],
  mode: EvidenceUploadMode,
): EvidenceFilesValidationResult {
  if (!files?.length) {
    return { ok: false, message: 'Select at least one file.' };
  }
  const maxMb = EVIDENCE_MAX_FILE_SIZE_BYTES / (1024 * 1024);
  for (const f of files) {
    if (!(f instanceof File)) {
      return { ok: false, message: 'Each item must be a file from your device or folder.' };
    }
    if (f.size === 0) {
      return { ok: false, message: 'Empty files cannot be uploaded. Choose a file with content.' };
    }
    if (f.size > EVIDENCE_MAX_FILE_SIZE_BYTES) {
      return {
        ok: false,
        message: `Each file must be at most ${maxMb} MB.`,
      };
    }
  }
  if (mode === 'single') {
    if (files.length > 1) {
      return { ok: false, message: 'Single upload allows only one file.' };
    }
    return { ok: true };
  }
  if (files.length > EVIDENCE_BULK_MAX_FILES) {
    return {
      ok: false,
      message: `Bulk upload allows at most ${EVIDENCE_BULK_MAX_FILES} files.`,
    };
  }
  return { ok: true };
}
