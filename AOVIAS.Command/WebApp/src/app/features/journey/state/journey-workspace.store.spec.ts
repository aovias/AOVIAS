import { HttpErrorResponse } from '@angular/common/http';
import { describe, expect, it } from 'vitest';
import { formatJourneyHttpError } from '../utils/journey-http-error';

/** Phase 56: store delegates to {@link formatJourneyHttpError} for workspace/chapter load failures. */
describe('JourneyWorkspaceStore HTTP messaging', () => {
  it('uses shared formatter for status 0', () => {
    expect(formatJourneyHttpError(new HttpErrorResponse({ status: 0 }))).toContain('Network interrupted');
  });
});
