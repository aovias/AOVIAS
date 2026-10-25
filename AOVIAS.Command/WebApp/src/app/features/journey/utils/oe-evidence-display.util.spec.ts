import {
  evidenceFileKindFromItem,
  evidenceFileKindFromMime,
  evidenceFileKindFromName,
  evidenceKindFromEvidenceDetailLike,
  evidenceTypeLabel,
  fileBasenameFromEvidenceUrl,
  parseHttpContentTypeHeader,
} from './oe-evidence-display.util';

describe('oe-evidence-display.util', () => {
  describe('evidenceFileKindFromName', () => {
    it('classifies pdf', () => {
      expect(evidenceFileKindFromName('report.PDF')).toBe('pdf');
      expect(evidenceFileKindFromName('a.pdf')).toBe('pdf');
    });
    it('classifies spreadsheet extensions', () => {
      expect(evidenceFileKindFromName('data.xlsx')).toBe('spreadsheet');
      expect(evidenceFileKindFromName('old.xls')).toBe('spreadsheet');
      expect(evidenceFileKindFromName('rows.csv')).toBe('spreadsheet');
    });
    it('classifies word extensions', () => {
      expect(evidenceFileKindFromName('memo.docx')).toBe('word');
      expect(evidenceFileKindFromName('legacy.doc')).toBe('word');
    });
    it('classifies image extensions', () => {
      expect(evidenceFileKindFromName('shot.PNG')).toBe('image');
      expect(evidenceFileKindFromName('photo.jpeg')).toBe('image');
      expect(evidenceFileKindFromName('icon.svg')).toBe('image');
    });
    it('returns other for unknown or missing extension', () => {
      expect(evidenceFileKindFromName('noext')).toBe('other');
      expect(evidenceFileKindFromName('data.zip')).toBe('other');
    });
  });

  describe('evidenceFileKindFromMime', () => {
    it('maps image/*', () => {
      expect(evidenceFileKindFromMime('image/png')).toBe('image');
      expect(evidenceFileKindFromMime('IMAGE/JPEG')).toBe('image');
    });
    it('maps application/pdf', () => {
      expect(evidenceFileKindFromMime('application/pdf')).toBe('pdf');
    });
  });

  describe('fileBasenameFromEvidenceUrl', () => {
    it('returns last segment', () => {
      expect(fileBasenameFromEvidenceUrl('https://x.com/a/b/photo.png')).toBe('photo.png');
      expect(fileBasenameFromEvidenceUrl('/files/evidence%201.jpeg')).toBe('evidence%201.jpeg');
    });
  });

  describe('parseHttpContentTypeHeader', () => {
    it('strips parameters', () => {
      expect(parseHttpContentTypeHeader('image/png; charset=utf-8')).toBe('image/png');
    });
  });

  describe('evidenceKindFromEvidenceDetailLike', () => {
    it('uses title with extension', () => {
      expect(
        evidenceKindFromEvidenceDetailLike({ title: 'x.png', evidenceType: 'Document' }),
      ).toBe('image');
    });
    it('uses name when title is absent', () => {
      expect(evidenceKindFromEvidenceDetailLike({ name: 'screenshot.png' })).toBe('image');
    });
    it('uses fileUrl basename', () => {
      expect(
        evidenceKindFromEvidenceDetailLike({
          title: 'No ext',
          fileUrl: 'https://x.com/a/b/c.jpeg',
        }),
      ).toBe('image');
    });
    it('uses versions[].fileUrl when main fileUrl missing', () => {
      expect(
        evidenceKindFromEvidenceDetailLike({
          title: 'Evidence',
          versions: [{ fileUrl: 'https://x.com/v/photo.webp' }],
        }),
      ).toBe('image');
    });
    it('uses evidenceType as MIME', () => {
      expect(evidenceKindFromEvidenceDetailLike({ evidenceType: 'image/webp' })).toBe('image');
    });
  });

  describe('evidenceFileKindFromItem', () => {
    it('uses name extension first', () => {
      expect(evidenceFileKindFromItem('a.png', {})).toBe('image');
    });
    it('uses fileUrl basename when name has no extension', () => {
      expect(
        evidenceFileKindFromItem('Holiday snapshot', {
          fileUrl: 'https://store/blob/abc/snapshot.png',
        }),
      ).toBe('image');
    });
    it('uses mime when name and url do not resolve', () => {
      expect(evidenceFileKindFromItem('Evidence file', { mimeType: 'image/png' })).toBe('image');
    });
    it('uses fileName when display name has no extension', () => {
      expect(
        evidenceFileKindFromItem('Uploaded evidence', {
          fileName: 'ward-round.png',
        }),
      ).toBe('image');
    });
  });

  describe('evidenceTypeLabel', () => {
    it('maps kinds to labels', () => {
      expect(evidenceTypeLabel('pdf')).toBe('PDF');
      expect(evidenceTypeLabel('spreadsheet')).toBe('Excel');
      expect(evidenceTypeLabel('word')).toBe('Word');
      expect(evidenceTypeLabel('image')).toBe('Image');
      expect(evidenceTypeLabel('other')).toBe('Document');
    });
  });
});
