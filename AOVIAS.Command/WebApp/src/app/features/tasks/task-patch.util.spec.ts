import { patchStatusForApi } from './task-patch.util';

describe('patchStatusForApi', () => {
  it('maps API labels to backend enum strings', () => {
    expect(patchStatusForApi('NotStarted')).toBe('Pending');
    expect(patchStatusForApi('InProgress')).toBe('InProgress');
    expect(patchStatusForApi('Blocked')).toBe('Blocked');
  });

  it('rejects Completed', () => {
    expect(() => patchStatusForApi('Completed')).toThrow();
  });
});
