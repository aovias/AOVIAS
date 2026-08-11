import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { TimeoutError } from 'rxjs';
import { formatJourneyHttpError } from './journey-http-error';

describe('formatJourneyHttpError', () => {
  it('maps RxJS TimeoutError', () => {
    expect(formatJourneyHttpError(new TimeoutError())).toContain('too long');
  });

  it('maps status 0 to network copy', () => {
    expect(formatJourneyHttpError(new HttpErrorResponse({ status: 0, statusText: 'Unknown Error' }))).toContain(
      'Network interrupted',
    );
  });

  it('maps 504', () => {
    expect(formatJourneyHttpError(new HttpErrorResponse({ status: 504, statusText: 'Gateway Timeout' }))).toContain(
      'too long',
    );
  });
});
