import {
  EVIDENCE_BULK_MAX_FILES,
  EVIDENCE_MAX_FILE_SIZE_BYTES,
  validateEvidenceFilesForMode,
} from './oe-evidence-upload.constants';

function fileNamed(name: string, size: number): File {
  return new File([new Uint8Array(size)], name, { type: 'application/octet-stream' });
}

describe('validateEvidenceFilesForMode', () => {
  it('rejects empty selection', () => {
    const r = validateEvidenceFilesForMode([], 'single');
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.message).toContain('Select');
  });

  it('single: rejects more than one file', () => {
    const r = validateEvidenceFilesForMode([fileNamed('a.pdf', 100), fileNamed('b.pdf', 100)], 'single');
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.message).toContain('one file');
  });

  it('single: accepts one file under max', () => {
    const r = validateEvidenceFilesForMode([fileNamed('a.pdf', 1024)], 'single');
    expect(r).toEqual({ ok: true });
  });

  it('rejects empty file (zero bytes)', () => {
    const r = validateEvidenceFilesForMode([fileNamed('empty.pdf', 0)], 'single');
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.message).toContain('Empty');
  });

  it('rejects file over max size', () => {
    const big = fileNamed('big.bin', EVIDENCE_MAX_FILE_SIZE_BYTES + 1);
    const r = validateEvidenceFilesForMode([big], 'bulk');
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.message).toContain('MB');
  });

  it('bulk: rejects more than max file count', () => {
    const files = Array.from({ length: EVIDENCE_BULK_MAX_FILES + 1 }, (_, i) =>
      fileNamed(`f${i}.pdf`, 10),
    );
    const r = validateEvidenceFilesForMode(files, 'bulk');
    expect(r.ok).toBe(false);
    if (!r.ok) expect(r.message).toContain(String(EVIDENCE_BULK_MAX_FILES));
  });

  it('bulk: accepts max count', () => {
    const files = Array.from({ length: EVIDENCE_BULK_MAX_FILES }, (_, i) => fileNamed(`f${i}.pdf`, 10));
    expect(validateEvidenceFilesForMode(files, 'bulk')).toEqual({ ok: true });
  });
});
