import {
  buildBulkEvidenceFormData,
  buildEvidenceVersionFormData,
  buildSingleEvidenceFormData,
} from './oe-evidence-upload.payload';

describe('oe-evidence-upload.payload', () => {
  it('buildSingleEvidenceFormData sets file, evidenceType, name, title, metadata JSON', () => {
    const file = new File(['hi'], 'r.pdf', { type: 'application/pdf' });
    const fd = buildSingleEvidenceFormData({
      file,
      evidenceType: 'Doc',
      title: 'T',
      metadata: { a: 1 },
    });
    expect(fd.get('evidenceType')).toBe('Doc');
    expect(fd.get('name')).toBe('T');
    expect(fd.get('title')).toBe('T');
    expect(fd.get('metadata')).toBe(JSON.stringify({ a: 1 }));
  });

  it('buildSingleEvidenceFormData uses file name when title omitted', () => {
    const file = new File(['x'], 'report.pdf', { type: 'application/pdf' });
    const fd = buildSingleEvidenceFormData({ file, evidenceType: 'Doc' });
    expect(fd.get('name')).toBe('report.pdf');
  });

  it('buildBulkEvidenceFormData repeats files field and sharedMetadata', () => {
    const f1 = new File(['1'], 'a.pdf');
    const f2 = new File(['2'], 'b.pdf');
    const fd = buildBulkEvidenceFormData({
      files: [f1, f2],
      evidenceType: 'X',
      sharedMetadata: { k: 'v' },
    });
    expect(fd.getAll('files').length).toBe(2);
    expect(fd.get('evidenceType')).toBe('X');
    expect(fd.get('sharedMetadata')).toBe(JSON.stringify({ k: 'v' }));
  });

  it('buildEvidenceVersionFormData sets file and optional notes', () => {
    const file = new File(['v'], 'v.pdf', { type: 'application/pdf' });
    const fd = buildEvidenceVersionFormData({ file, notes: 'rev b' });
    const part = fd.get('file');
    expect(part instanceof File).toBe(true);
    expect((part as File).name).toBe('v.pdf');
    expect(fd.get('notes')).toBe('rev b');
  });
});
