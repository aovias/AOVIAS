import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { AiPageContextService } from '../../../../core/services/ai-page-context.service';
import { AuditContextPanelService } from './audit-context-panel.service';
import { AuditWorkspaceActionsService } from './audit-workspace-actions.service';
import {
  AuditWorkspaceParticipationService,
  injectAuditWorkspace,
} from './audit-workspace-participation.service';

/** Stands in for a routed page that participates in the workspace. */
@Component({ standalone: true, template: '' })
class HostPageComponent {
  readonly workspace = injectAuditWorkspace();
}

describe('AuditWorkspaceParticipationService', () => {
  let registry: AuditWorkspaceParticipationService;
  let panel: AuditContextPanelService;
  let actions: AuditWorkspaceActionsService;
  let assistant: AiPageContextService;

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({});
    registry = TestBed.inject(AuditWorkspaceParticipationService);
    panel = TestBed.inject(AuditContextPanelService);
    actions = TestBed.inject(AuditWorkspaceActionsService);
    assistant = TestBed.inject(AiPageContextService);
  });

  it('fans a single declaration out to the panel, the toolbar and the assistant', () => {
    const owner = Symbol('page');
    registry.declare(owner, {
      stage: 'findings',
      subject: { kind: 'finding', id: 'f-1', title: 'OBS-08-1' },
      actions: [{ id: 'a', label: 'Create CAPA', run: () => undefined }],
    });

    expect(panel.subject().kind).toBe('finding');
    expect(actions.actionsFor('findings')).toHaveLength(1);
    expect(assistant.snapshot().pageContext).toBe('Findings');
    expect(assistant.snapshot().entityId).toBe('f-1');
  });

  it('maps CAPA subjects onto the assistant’s existing Dcc context', () => {
    registry.declare(Symbol('page'), {
      stage: 'capa',
      subject: { kind: 'capa', id: 'c-1' },
    });

    expect(assistant.snapshot().pageContext).toBe('Dcc');
  });

  it('replaces the previous declaration wholesale rather than merging', () => {
    const first = Symbol('first');
    registry.declare(first, {
      stage: 'findings',
      subject: { kind: 'finding' },
      documents: [{ id: 'd1', title: 'SOP-12', kind: 'SOP' }],
    });

    registry.declare(Symbol('second'), {
      stage: 'capa',
      subject: { kind: 'capa' },
    });

    // The first page's documents must not linger under the second's subject.
    expect(registry.documents()).toEqual([]);
    expect(registry.participation().stage).toBe('capa');
  });

  it('sorts the timeline newest first', () => {
    registry.declare(Symbol('page'), {
      stage: 'execution',
      subject: { kind: 'audit' },
      timeline: [
        { id: 'a', label: 'Created', at: '2026-01-01T00:00:00Z' },
        { id: 'c', label: 'Closed', at: '2026-03-01T00:00:00Z' },
        { id: 'b', label: 'Started', at: '2026-02-01T00:00:00Z' },
      ],
    });

    expect(registry.timeline().map((e) => e.id)).toEqual(['c', 'b', 'a']);
  });

  it('derives the AI page context from the subject when none is declared', () => {
    registry.declare(Symbol('page'), {
      stage: 'evidence',
      subject: { kind: 'evidence' },
    });

    expect(registry.aiContext().pageContext).toBe('AuditEvidence');
  });

  it('carries declared facts and record status into the AI context', () => {
    registry.declare(Symbol('page'), {
      stage: 'findings',
      subject: { kind: 'finding' },
      workflowState: { stage: 'findings', statusLabel: 'Open' },
      aiContext: { department: 'ICU', facts: { openFindings: '4' } },
    });

    const ctx = registry.aiContext();
    expect(ctx.department).toBe('ICU');
    expect(ctx.facts['openFindings']).toBe('4');
    expect(ctx.statusLabel).toBe('Open');
  });

  it('ignores a withdrawal from a page that no longer owns the workspace', () => {
    const outgoing = Symbol('outgoing');
    registry.declare(outgoing, { stage: 'findings', subject: { kind: 'finding' } });

    const incoming = Symbol('incoming');
    registry.declare(incoming, { stage: 'capa', subject: { kind: 'capa' } });

    // Angular builds the next route component before destroying the previous
    // one, so this late teardown must not wipe the incoming page's context.
    registry.withdraw(outgoing);

    expect(registry.participation().stage).toBe('capa');
    expect(panel.subject().kind).toBe('capa');
  });

  it('clears everything when the owning page withdraws', () => {
    const owner = Symbol('page');
    registry.declare(owner, {
      stage: 'findings',
      subject: { kind: 'finding' },
      actions: [{ id: 'a', label: 'Act', run: () => undefined }],
    });

    registry.withdraw(owner);

    expect(registry.participation().stage).toBeNull();
    expect(panel.subject().kind).toBe('none');
    expect(actions.actionsFor('findings')).toEqual([]);
    expect(assistant.snapshot().pageContext).toBe('General');
  });

  it('withdraws automatically when the host component is destroyed', () => {
    const fixture = TestBed.createComponent(HostPageComponent);
    fixture.componentInstance.workspace.set({
      stage: 'execution',
      subject: { kind: 'audit', title: 'ICU audit' },
    });
    expect(registry.participation().stage).toBe('execution');

    // No ngOnDestroy anywhere — DestroyRef drives the cleanup.
    fixture.destroy();

    expect(registry.participation().stage).toBeNull();
    expect(panel.subject().kind).toBe('none');
  });
});
