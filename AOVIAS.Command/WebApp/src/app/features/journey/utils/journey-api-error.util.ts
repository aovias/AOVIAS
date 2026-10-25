import { HttpErrorResponse } from '@angular/common/http';

/**
 * Best-effort user-facing message from failed journey API calls.
 */
export function journeyApiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof HttpErrorResponse) {
    const e = err.error;
    if (typeof e === 'string' && e.trim()) return e.trim();
    if (e && typeof e === 'object') {
      const rec = e as Record<string, unknown>;
      if (typeof rec['message'] === 'string' && (rec['message'] as string).trim()) {
        return (rec['message'] as string).trim();
      }
      const errs = rec['errors'];
      if (Array.isArray(errs) && errs.length && typeof errs[0] === 'string') {
        return String(errs[0]);
      }
      if (typeof errs === 'string' && errs.trim()) return errs.trim();
    }
    if (err.status === 0) {
      return 'Network error. Check your connection and try again.';
    }
    if (err.status >= 500) {
      return 'Server error. Please try again later.';
    }
  }
  return fallback;
}
