import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuditWorkspaceStateService } from './audit-workspace-state.service';

describe('AuditWorkspaceStateService', () => {
  let service: AuditWorkspaceStateService;

  beforeEach(() => {
    // Each test starts from a clean slate; the service restores from
    // sessionStorage at construction, so it must be cleared *before* injection.
    sessionStorage.clear();
    localStorage.clear();
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [AuditWorkspaceStateService] });
    service = TestBed.inject(AuditWorkspaceStateService);
  });

  it('starts with no audit bound', () => {
    expect(service.hasActiveAudit()).toBe(false);
    expect(service.sessionId()).toBeNull();
  });

  it('binds an audit and its originating scheduled audit', () => {
    service.setActiveAudit('session-1', 'scheduled-1');

    expect(service.sessionId()).toBe('session-1');
    expect(service.scheduledAuditId()).toBe('scheduled-1');
    expect(service.hasActiveAudit()).toBe(true);
  });

  it('drops downstream selections when a different audit is opened', () => {
    service.setActiveAudit('session-1', 'scheduled-1');
    service.setClause('clause-9');
    service.setFinding('finding-9');
    service.setEvidence('evidence-9');

    service.setActiveAudit('session-2', 'scheduled-2');

    // Carrying these over would show records belonging to the previous audit.
    expect(service.clauseId()).toBeNull();
    expect(service.findingId()).toBeNull();
    expect(service.evidenceId()).toBeNull();
    expect(service.sessionId()).toBe('session-2');
  });

  it('keeps the department selection across a tab switch', () => {
    service.setDepartment('dept-3');
    service.setActiveTab('findings');

    expect(service.departmentId()).toBe('dept-3');
  });

  it('restores the selection context for a new instance in the same session', () => {
    service.setActiveAudit('session-7', 'scheduled-7');
    service.setDepartment('dept-7');

    // Simulates a reload: fresh instance, same sessionStorage.
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [AuditWorkspaceStateService] });
    const revived = TestBed.inject(AuditWorkspaceStateService);

    expect(revived.sessionId()).toBe('session-7');
    expect(revived.departmentId()).toBe('dept-7');
  });

  it('fills in keys missing from a context persisted by an older build', () => {
    sessionStorage.setItem(
      'aovias.audit.workspace.context.v1',
      JSON.stringify({ sessionId: 'session-legacy' }),
    );

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [AuditWorkspaceStateService] });
    const revived = TestBed.inject(AuditWorkspaceStateService);

    expect(revived.sessionId()).toBe('session-legacy');
    expect(revived.evidenceId()).toBeNull();
  });

  it('survives a corrupt persisted context instead of failing to construct', () => {
    sessionStorage.setItem('aovias.audit.workspace.context.v1', '{not json');

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [AuditWorkspaceStateService] });
    const revived = TestBed.inject(AuditWorkspaceStateService);

    expect(revived.hasActiveAudit()).toBe(false);
  });

  it('derives the lifecycle stage from the active tab', () => {
    service.setActiveTab('capa');
    expect(service.currentStage()?.id).toBe('capa');
    expect(service.suggestedNextStage()?.id).toBe('reports');
  });

  it('reports no stage while on a tab outside the lifecycle', () => {
    service.setActiveTab('administration');
    expect(service.currentStage()).toBeNull();
    expect(service.suggestedNextStage()).toBeNull();
  });

  it('clears the context only on an explicit reset', () => {
    service.setActiveAudit('session-1', 'scheduled-1');
    service.resetContext();

    expect(service.hasActiveAudit()).toBe(false);
    expect(service.scheduledAuditId()).toBeNull();
  });
});
