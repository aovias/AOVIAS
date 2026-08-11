import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { copilotPageContextFor } from '../models/audit-context-panel.model';
import { AuditContextPanelService } from './audit-context-panel.service';

describe('AuditContextPanelService', () => {
  let service: AuditContextPanelService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [AuditContextPanelService] });
    service = TestBed.inject(AuditContextPanelService);
  });

  it('starts with no subject and nothing to show', () => {
    expect(service.subject().kind).toBe('none');
    expect(service.sections()).toEqual([]);
    expect(service.hasContent()).toBe(false);
  });

  it('drops the previous sections when the subject changes', () => {
    service.setSubject({ kind: 'finding', id: 'f-1', title: 'OBS-08-1' });
    service.setSections([{ id: 's1', title: 'Linked documents', items: [{ label: 'SOP-12' }] }]);

    service.setSubject({ kind: 'evidence', id: 'e-1' });

    // Keeping them would attribute the finding's documents to the evidence.
    expect(service.sections()).toEqual([]);
    expect(service.subject().kind).toBe('evidence');
  });

  it('reports content once a subject is set', () => {
    service.setSubject({ kind: 'audit', id: 'a-1', title: 'ICU Internal Audit' });
    expect(service.hasContent()).toBe(true);
  });

  it('clears back to the empty state', () => {
    service.setSubject({ kind: 'capa', id: 'c-1' });
    service.setSections([{ id: 's', title: 'Related', items: [] }]);

    service.clear();

    expect(service.subject().kind).toBe('none');
    expect(service.sections()).toEqual([]);
  });

  it('defaults to open so the panel is discoverable', () => {
    expect(service.isOpen()).toBe(true);
  });

  it('remembers the collapsed preference across instances', () => {
    service.setOpen(false);

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [AuditContextPanelService] });
    const revived = TestBed.inject(AuditContextPanelService);

    expect(revived.isOpen()).toBe(false);
  });

  it('toggles open and closed', () => {
    const before = service.isOpen();
    service.toggle();
    expect(service.isOpen()).toBe(!before);
  });
});

describe('copilotPageContextFor', () => {
  it('maps each subject kind to the page context the Copilot API keys on', () => {
    expect(copilotPageContextFor({ kind: 'finding' })).toBe('AuditFindings');
    expect(copilotPageContextFor({ kind: 'evidence' })).toBe('AuditEvidence');
    expect(copilotPageContextFor({ kind: 'planning' })).toBe('AuditPlanning');
    expect(copilotPageContextFor({ kind: 'capa' })).toBe('AuditCapa');
    expect(copilotPageContextFor({ kind: 'audit' })).toBe('AuditExecution');
    expect(copilotPageContextFor({ kind: 'clause' })).toBe('AuditAssessment');
  });

  it('falls back to the general context when nothing is selected', () => {
    expect(copilotPageContextFor({ kind: 'none' })).toBe('General');
  });
});
