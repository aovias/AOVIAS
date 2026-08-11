import { HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

/**
 * User-safe message for evidence upload failures (multipart / Gateway validation).
 * Logs full error when `environment.enableDebugLogging` is true.
 */
export function formatEvidenceHttpError(err: unknown): string {
  if (environment.enableDebugLogging && err instanceof HttpErrorResponse) {
    // eslint-disable-next-line no-console -- intentional debug diagnostics
    console.debug('[Evidence HTTP]', {
      status: err.status,
      url: err.url,
      error: err.error,
    });
  }

  if (!(err instanceof HttpErrorResponse)) {
    return 'Upload failed. Please try again.';
  }

  if (err.status === 403) {
    return 'You do not have permission to perform this action.';
  }
  if (err.status === 404) {
    return 'Journey or resource was not found.';
  }
  if (err.status === 413) {
    return 'File is too large (max 10 MB).';
  }

  const body = err.error;
  if (body && typeof body === 'object' && 'errors' in body) {
    const raw = (body as { errors?: unknown }).errors;
    if (raw && typeof raw === 'object' && !Array.isArray(raw)) {
      const dict = raw as Record<string, unknown>;
      for (const key of Object.keys(dict)) {
        const msgs = dict[key];
        if (Array.isArray(msgs) && msgs.length > 0 && typeof msgs[0] === 'string') {
          const s = msgs[0].trim();
          if (s.length > 0 && s.length <= 240) {
            return s;
          }
        }
      }
      const keys = Object.keys(dict);
      if (keys.length > 0) {
        return 'Upload validation failed. Check your file selection and evidence type, then try again.';
      }
    }
  }

  if (body && typeof body === 'object' && 'title' in body) {
    const title = (body as { title?: string }).title;
    if (typeof title === 'string' && title.length > 0 && title.length <= 120) {
      return title;
    }
  }

  const message =
    body && typeof body === 'object' && 'message' in body
      ? (body as { message?: string }).message
      : undefined;
  if (typeof message === 'string' && message.length > 0 && message.length <= 160) {
    return message;
  }

  return 'Upload failed. Please try again.';
}
