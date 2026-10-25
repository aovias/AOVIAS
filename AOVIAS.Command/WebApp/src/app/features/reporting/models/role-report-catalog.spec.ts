import { describe, expect, it } from 'vitest';
import {
  DEFAULT_REPORT_PACK_SETS,
  filterReportCatalogByPack,
  normalizePackId,
  resolveDeptHeadPackFromRoles,
  ROLE_REPORT_CATALOG,
} from './role-report-catalog';

describe('role-report-catalog', () => {
  it('normalizes unknown packs to quality-manager', () => {
    expect(normalizePackId(undefined)).toBe('quality-manager');
    expect(normalizePackId('staff')).toBe('quality-manager');
    expect(normalizePackId('Department-Head')).toBe('department-head');
  });

  it('filters Owner / hospital-admin default set', () => {
    const ids = filterReportCatalogByPack('hospital-admin').map((r) => r.id);
    expect(ids).toEqual(expect.arrayContaining(DEFAULT_REPORT_PACK_SETS['hospital-admin']));
    expect(ids).not.toContain('evidence');
  });

  it('filters Quality Manager to include evidence and tasks', () => {
    const ids = filterReportCatalogByPack('quality-manager').map((r) => r.id);
    expect(ids).toContain('tasks');
    expect(ids).toContain('evidence');
    expect(ids).toContain('compliance');
  });

  it('filters Department Head to tasks + evidence only', () => {
    const ids = filterReportCatalogByPack('department-head').map((r) => r.id);
    expect(ids.sort()).toEqual(['evidence', 'tasks']);
  });

  it('filters Consultant portfolio set', () => {
    const ids = filterReportCatalogByPack('consultant').map((r) => r.id);
    expect(ids).toEqual(expect.arrayContaining(['compliance', 'audit', 'readiness', 'custom']));
    expect(ids).not.toContain('tasks');
  });

  it('filters Auditor to audit + evidence', () => {
    const ids = filterReportCatalogByPack('auditor').map((r) => r.id);
    expect(ids.sort()).toEqual(['audit', 'evidence']);
  });

  it('catalog covers all documented report categories', () => {
    const ids = ROLE_REPORT_CATALOG.map((r) => r.id);
    expect(ids).toEqual(
      expect.arrayContaining(['compliance', 'audit', 'tasks', 'evidence', 'readiness', 'custom']),
    );
  });
});

describe('resolveDeptHeadPackFromRoles', () => {
  it('resolves Department Head aliases before manager-style packs', () => {
    expect(resolveDeptHeadPackFromRoles(['MANAGER', 'Department Head'])).toBe('department-head');
    expect(resolveDeptHeadPackFromRoles(['DEPARTMENT_HEAD'])).toBe('department-head');
    expect(resolveDeptHeadPackFromRoles(['DeptHead'])).toBe('department-head');
    expect(resolveDeptHeadPackFromRoles(['MANAGER'])).toBeNull();
  });
});
