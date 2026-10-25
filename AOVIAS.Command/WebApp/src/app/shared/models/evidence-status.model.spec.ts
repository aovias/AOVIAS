import {
  evidenceStatusLabel,
  normalizeEvidenceStatus,
} from './evidence-status.model';

describe('evidence-status.model', () => {
  it('maps verified alias to Approved for evidence items', () => {
    expect(evidenceStatusLabel('verified', 'evidence')).toBe('Approved');
    expect(normalizeEvidenceStatus('verified', 'evidence')).toBe('approved');
  });

  it('maps verified to Verified for obligation context', () => {
    expect(evidenceStatusLabel('verified', 'obligation')).toBe('Verified');
  });

  it('maps returnForRevision to Needs More Evidence', () => {
    expect(evidenceStatusLabel('returnForRevision')).toBe('Needs More Evidence');
  });
});
