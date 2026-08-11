import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import type { ContextDrawerSection } from './context-drawer.model';
import { ContextDrawerService } from './context-drawer.service';

const section = (id: string): ContextDrawerSection => ({
  id,
  label: id,
  glyph: '·',
  load: () => Promise.resolve(class {}),
});

describe('ContextDrawerService', () => {
  let service: ContextDrawerService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [ContextDrawerService] });
    service = TestBed.inject(ContextDrawerService);
  });

  it('starts closed and unpinned so the workspace opens at full width', () => {
    expect(service.isOpen()).toBe(false);
    expect(service.pinned()).toBe(false);
  });

  it('reports no sections until a workspace contributes some', () => {
    expect(service.hasSections()).toBe(false);
    expect(service.activeSection()).toBeNull();
  });

  it('activates the first section when sections arrive', () => {
    service.setSections([section('ai'), section('docs')]);
    expect(service.activeSection()?.id).toBe('ai');
  });

  it('opens directly at a requested section', () => {
    service.setSections([section('ai'), section('docs')]);
    service.open('docs');

    expect(service.isOpen()).toBe(true);
    expect(service.activeSection()?.id).toBe('docs');
  });

  it('toggles closed when the active section is pressed again', () => {
    service.setSections([section('ai'), section('docs')]);
    service.toggle('docs');
    expect(service.isOpen()).toBe(true);

    service.toggle('docs');
    expect(service.isOpen()).toBe(false);
  });

  it('switches section rather than closing when a different button is pressed', () => {
    service.setSections([section('ai'), section('docs')]);
    service.toggle('ai');
    service.toggle('docs');

    expect(service.isOpen()).toBe(true);
    expect(service.activeSection()?.id).toBe('docs');
  });

  it('treats pinned as open so the two can never contradict', () => {
    service.setSections([section('ai')]);
    service.setPinned(true);

    expect(service.isOpen()).toBe(true);
    // close() alone must not defeat an explicit pin.
    service.close();
    expect(service.isOpen()).toBe(true);
  });

  it('keeps the user on their section while moving between records', () => {
    service.setSections([section('ai'), section('docs')]);
    service.select('docs');

    // A new page offering the same sections should not reset to the first tab.
    service.setSections([section('ai'), section('docs'), section('timeline')]);

    expect(service.activeSection()?.id).toBe('docs');
  });

  it('falls back to the first section when the remembered one is not offered', () => {
    service.setSections([section('ai'), section('docs')]);
    service.select('docs');

    service.setSections([section('ai'), section('timeline')]);

    expect(service.activeSection()?.id).toBe('ai');
  });

  it('ignores selection of a section the workspace did not offer', () => {
    service.setSections([section('ai')]);
    service.select('nonexistent');

    expect(service.activeSection()?.id).toBe('ai');
  });

  it('clears the active section when a workspace offers none', () => {
    service.setSections([section('ai')]);
    service.setSections([]);

    expect(service.hasSections()).toBe(false);
    expect(service.activeSection()).toBeNull();
  });

  it('persists the pinned preference across instances', () => {
    service.setPinned(true);

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [ContextDrawerService] });
    const revived = TestBed.inject(ContextDrawerService);

    expect(revived.pinned()).toBe(true);
  });

  it('persists the chosen section across instances', () => {
    service.setSections([section('ai'), section('docs')]);
    service.select('docs');

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [ContextDrawerService] });
    const revived = TestBed.inject(ContextDrawerService);
    revived.setSections([section('ai'), section('docs')]);

    expect(revived.activeSection()?.id).toBe('docs');
  });

  it('unpins when the pinned drawer is toggled shut from its own button', () => {
    service.setSections([section('ai')]);
    service.setPinned(true);

    service.toggle('ai');

    expect(service.pinned()).toBe(false);
    expect(service.isOpen()).toBe(false);
  });
});
