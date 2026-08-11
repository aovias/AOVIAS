import { environment } from '../../../../environments/environment';

/**
 * Logs FormData field keys and, for each file part, name/size/type (dev diagnostics only).
 * Does not read full binary into memory beyond what `File` already holds.
 */
export function logEvidenceFormDataDebug(fd: FormData, context?: string): void {
  if (!environment.enableDebugLogging) return;

  const parts: Array<{
    key: string;
    kind: 'file' | 'field';
    name?: string;
    size?: number;
    type?: string;
    valuePreview?: string;
  }> = [];

  fd.forEach((value, key) => {
    if (value instanceof File) {
      parts.push({
        key,
        kind: 'file',
        name: value.name,
        size: value.size,
        type: value.type || '(no type)',
      });
    } else {
      const s = typeof value === 'string' ? value : String(value);
      parts.push({
        key,
        kind: 'field',
        valuePreview: s.length > 200 ? `${s.slice(0, 200)}…` : s,
      });
    }
  });

  // eslint-disable-next-line no-console -- intentional debug diagnostics
  console.debug('[OE Evidence] FormData', context ?? '', parts);
}
