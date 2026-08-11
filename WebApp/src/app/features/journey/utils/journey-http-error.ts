import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';

/** User-facing copy for Journey workspace / long-read failures (no automatic retries). */
export function formatJourneyHttpError(err: unknown): string {
  if (err instanceof TimeoutError) {
    return 'This request took too long. Check your connection or try again.';
  }
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) {
      return 'Network interrupted or the request could not complete. Try again or refresh the page.';
    }
    if (err.status === 403) {
      return 'You do not have permission to perform this action.';
    }
    if (err.status === 404) {
      return 'Journey or resource was not found.';
    }
    if (err.status === 409) {
      return 'Conflict: ambiguous or duplicate reference (for example, more than one knowledge-base node shares this code).';
    }
    if (err.status === 413) {
      return 'File is too large (max 10 MB).';
    }
    if (err.status === 423) {
      const body423 = err.error as { message?: string; blockingReasons?: string[] } | null;
      const reasons = body423?.blockingReasons?.filter(Boolean);
      if (reasons?.length) {
        return `This maturity stage is locked: ${reasons.join(' ')}`;
      }
      return body423?.message ?? 'This maturity stage is locked. Complete the previous stage to continue.';
    }
    if (err.status === 504) {
      return 'The server took too long to respond. Try again in a moment.';
    }
    const body = err.error as { message?: string } | null;
    return body?.message ?? err.message ?? 'Request failed.';
  }
  return 'Request failed.';
}
