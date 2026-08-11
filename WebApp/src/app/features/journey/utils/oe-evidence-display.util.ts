/** Filename-derived family for icon + user-facing type label (OE wizard evidence list). */
export type EvidenceFileKind = 'pdf' | 'spreadsheet' | 'word' | 'image' | 'other';

const IMAGE_EXTENSIONS = new Set([
  'png',
  'jpg',
  'jpeg',
  'gif',
  'webp',
  'bmp',
  'svg',
  'ico',
  'tif',
  'tiff',
  'avif',
  'heic',
  'heif',
]);

/** Last path segment of a URL, for inferring extension when `name` has none (Gateway often omits extension on `name`). */
export function fileBasenameFromEvidenceUrl(url: string): string {
  const t = url.trim();
  if (!t) return '';
  const noQuery = t.replace(/[?#].*$/, '');
  const seg = noQuery.split(/[/\\]/).pop() ?? '';
  return seg;
}

/**
 * Maps HTTP Content-Type to our kind when filename has no extension.
 */
export function evidenceFileKindFromMime(mime: string): EvidenceFileKind | null {
  const m = mime.trim().toLowerCase();
  if (!m) return null;
  if (m.startsWith('image/')) return 'image';
  if (m === 'application/pdf') return 'pdf';
  if (
    m === 'text/csv' ||
    m === 'application/vnd.ms-excel' ||
    m.includes('spreadsheetml') ||
    m === 'application/vnd.oasis.opendocument.spreadsheet'
  ) {
    return 'spreadsheet';
  }
  if (
    m === 'application/msword' ||
    m.includes('wordprocessingml') ||
    m === 'application/rtf' ||
    m === 'application/vnd.oasis.opendocument.text'
  ) {
    return 'word';
  }
  return null;
}

export function evidenceFileKindFromName(fileName: string): EvidenceFileKind {
  const parts = fileName.trim().split('.');
  const ext = parts.length > 1 ? (parts.pop() ?? '').toLowerCase() : '';
  if (ext === 'pdf') return 'pdf';
  if (['xlsx', 'xls', 'csv', 'ods'].includes(ext)) return 'spreadsheet';
  if (['doc', 'docx', 'odt', 'rtf'].includes(ext)) return 'word';
  if (IMAGE_EXTENSIONS.has(ext)) return 'image';
  return 'other';
}

export interface EvidenceKindHints {
  mimeType?: string | null;
  fileUrl?: string | null;
  /**
   * Stored / technical filename when `name` is a display title without extension
   * (Gateway may send both).
   */
  fileName?: string | null;
}

/**
 * Prefer extension on `name`; then basename of `fileUrl`; then `mimeType`.
 * Use this for list rows — APIs often return a display `name` without extension.
 */
export function evidenceFileKindFromItem(name: string, hints?: EvidenceKindHints): EvidenceFileKind {
  const fromName = evidenceFileKindFromName(name);
  if (fromName !== 'other') return fromName;

  const hintFile = hints?.fileName?.trim();
  if (hintFile) {
    const fromHintFile = evidenceFileKindFromName(hintFile);
    if (fromHintFile !== 'other') return fromHintFile;
  }

  const urlBase = hints?.fileUrl ? fileBasenameFromEvidenceUrl(hints.fileUrl) : '';
  if (urlBase) {
    const fromUrl = evidenceFileKindFromName(urlBase);
    if (fromUrl !== 'other') return fromUrl;
  }

  const mime = hints?.mimeType?.trim();
  if (mime) {
    const fromMime = evidenceFileKindFromMime(mime);
    if (fromMime) return fromMime;
    const fromLoose = evidenceKindFromLooseDescriptor(mime);
    if (fromLoose) return fromLoose;
  }

  return 'other';
}

export function evidenceTypeLabel(kind: EvidenceFileKind): string {
  switch (kind) {
    case 'pdf':
      return 'PDF';
    case 'spreadsheet':
      return 'Excel';
    case 'word':
      return 'Word';
    case 'image':
      return 'Image';
    default:
      return 'Document';
  }
}

export function evidenceTypeLabelForItem(name: string, hints?: EvidenceKindHints): string {
  return evidenceTypeLabel(evidenceFileKindFromItem(name, hints));
}

/** Primary part of `Content-Type` (strip parameters). */
export function parseHttpContentTypeHeader(value: string | null | undefined): string | null {
  if (value == null) return null;
  const main = String(value).split(';')[0].trim().toLowerCase();
  return main.length ? main : null;
}

/** When headers / `Blob.type` are wrong (e.g. `application/octet-stream`), infer from magic bytes. */
export async function probeEvidenceKindFromBlobPrefix(blob: Blob): Promise<EvidenceFileKind | null> {
  const n = Math.min(blob.size, 64);
  if (n < 4) return null;
  const buf = await blob.slice(0, n).arrayBuffer();
  const u = new Uint8Array(buf);
  if (u.length >= 8 && u[0] === 0x89 && u[1] === 0x50 && u[2] === 0x4e && u[3] === 0x47) return 'image';
  if (u.length >= 3 && u[0] === 0xff && u[1] === 0xd8 && u[2] === 0xff) return 'image';
  if (u.length >= 6 && u[0] === 0x47 && u[1] === 0x49 && u[2] === 0x46 && u[3] === 0x38) return 'image';
  if (
    u.length >= 12 &&
    u[0] === 0x52 &&
    u[1] === 0x49 &&
    u[2] === 0x46 &&
    u[3] === 0x46 &&
    u[8] === 0x57 &&
    u[9] === 0x45 &&
    u[10] === 0x42 &&
    u[11] === 0x50
  ) {
    return 'image';
  }
  if (u.length >= 4 && u[0] === 0x25 && u[1] === 0x50 && u[2] === 0x44 && u[3] === 0x46) return 'pdf';
  return null;
}

/** Subset of Gateway evidence detail — avoids importing journey-api here. */
export interface EvidenceDetailLike {
  title?: string | null;
  /** Many gateways use `name` for the file / display row; may include an extension when `title` does not. */
  name?: string | null;
  fileName?: string | null;
  originalFileName?: string | null;
  evidenceType?: string | null;
  fileUrl?: string | null;
  mimeType?: string | null;
  contentType?: string | null;
  versions?: ReadonlyArray<{ fileUrl?: string | null }> | null;
}

/** Product / category strings (and MIME) for `evidenceType` or non-standard type fields. */
function evidenceKindFromLooseDescriptor(raw: string): EvidenceFileKind | null {
  const et = raw.trim();
  if (!et) return null;
  const fromMime = evidenceFileKindFromMime(et);
  if (fromMime) return fromMime;
  const lower = et.toLowerCase();
  if (lower === 'image' || lower.includes('photo') || lower.includes('picture')) return 'image';
  if (lower.includes('pdf')) return 'pdf';
  if (lower.includes('spreadsheet') || lower.includes('excel')) return 'spreadsheet';
  if (
    lower === 'word' ||
    lower.includes('msword') ||
    lower.includes('wordprocessing') ||
    lower.includes('ms-word')
  ) {
    return 'word';
  }
  return null;
}

/**
 * Uses filenames on **title** / **name** / **fileName**, **fileUrl** (and version URLs), then MIME / type strings.
 */
export function evidenceKindFromEvidenceDetailLike(d: EvidenceDetailLike | null | undefined): EvidenceFileKind {
  if (!d) return 'other';

  const nameCandidates = [d.title, d.name, d.fileName, d.originalFileName];
  for (const raw of nameCandidates) {
    const title = (raw ?? '').trim();
    if (title) {
      const k = evidenceFileKindFromName(title);
      if (k !== 'other') return k;
    }
  }

  const urlCandidates: string[] = [];
  const mainUrl = (d.fileUrl ?? '').trim();
  if (mainUrl) urlCandidates.push(mainUrl);
  if (Array.isArray(d.versions)) {
    for (const v of d.versions) {
      const u = (v?.fileUrl ?? '').trim();
      if (u) urlCandidates.push(u);
    }
  }
  for (const url of urlCandidates) {
    const base = fileBasenameFromEvidenceUrl(url);
    const k = evidenceFileKindFromName(base);
    if (k !== 'other') return k;
  }

  const mimeRaw = d.mimeType ?? d.contentType;
  const mime = (mimeRaw ?? '').trim();
  if (mime) {
    const fromLoose = evidenceKindFromLooseDescriptor(mime);
    if (fromLoose) return fromLoose;
  }

  const et = (d.evidenceType ?? '').trim();
  if (et) {
    const fromLoose = evidenceKindFromLooseDescriptor(et);
    if (fromLoose) return fromLoose;
  }

  return 'other';
}
