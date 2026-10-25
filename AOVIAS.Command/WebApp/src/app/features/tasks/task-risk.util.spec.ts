import { computeRiskScore, isOverdue, toRiskLevel } from './task-risk.util';

describe('task-risk.util', () => {
  const today = new Date(Date.UTC(2026, 3, 21));

  it('matches backend low + low + not overdue = 2', () => {
    const s = computeRiskScore('2026-05-01', 'NotStarted', 'Low', 'Low', today);
    expect(s).toBe(2);
    expect(toRiskLevel(s)).toBe('Low');
  });

  it('overdue adds 2', () => {
    const s = computeRiskScore('2026-04-10', 'InProgress', 'Low', 'Low', today);
    expect(s).toBe(4);
    expect(toRiskLevel(s)).toBe('Medium');
  });

  it('completed not overdue', () => {
    expect(isOverdue('2020-01-01', 'Completed', today)).toBe(false);
  });
});
