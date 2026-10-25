import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';
import { FindingsDetailPanelComponent } from './findings-detail-panel.component';
import { FindingsWorkspaceService } from '../services/findings-workspace.service';
import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';
import type { FindingsWorkspaceDetail } from '../models/findings-workspace.model';

/**
 * DEF-M3-001 — compliance mapping was write-once at creation, so a finding raised without an
 * objective element could be accepted and then never promoted. These tests pin *when* the mapping
 * control is offered: it must be available on a live finding, and withheld once the record is
 * settled.
 */
describe('FindingsDetailPanelComponent — compliance mapping (DEF-M3-001)', () => {
  const detailOf = (over: Partial<FindingsWorkspaceDetail>) =>
    ({
      status: 'Accepted',
      promotedDeficiencyId: null,
      ...over,
    }) as FindingsWorkspaceDetail;

  const make = () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        {
          provide: FindingsWorkspaceService,
          useValue: { searchComplianceContext: vi.fn(() => of([])) },
        },
        {
          provide: PlatformConfigurationService,
          useValue: {
            optionsWith: () => () => [],
            options: () => () => [],
            label: (_c: string, _v: string, f: string) => f,
            labelByOrdinalOrCode: (_c: string, v: string) => v,
          },
        },
      ],
    });
    return TestBed.createComponent(FindingsDetailPanelComponent).componentInstance;
  };

  it('offers mapping on an accepted finding — the case the defect stranded', () => {
    make();
    const fixture = TestBed.createComponent(FindingsDetailPanelComponent);
    fixture.componentRef.setInput('detail', detailOf({ status: 'Accepted' }));

    expect(fixture.componentInstance.canMap()).toBe(true);
  });

  it('withholds mapping once the finding is closed', () => {
    make();
    const fixture = TestBed.createComponent(FindingsDetailPanelComponent);
    fixture.componentRef.setInput('detail', detailOf({ status: 'Closed' }));

    expect(fixture.componentInstance.canMap()).toBe(false);
  });

  it('withholds mapping once a CAPA hangs off the mapping', () => {
    make();
    const fixture = TestBed.createComponent(FindingsDetailPanelComponent);
    fixture.componentRef.setInput(
      'detail',
      detailOf({ status: 'Accepted', promotedDeficiencyId: 'def-1' }),
    );

    expect(fixture.componentInstance.canMap()).toBe(false);
  });

  it('emits the chosen objective element for the container to persist', () => {
    make();
    const fixture = TestBed.createComponent(FindingsDetailPanelComponent);
    fixture.componentRef.setInput('detail', detailOf({}));
    const emitted: string[] = [];
    fixture.componentInstance.complianceContextMapped.subscribe((id) => emitted.push(id));

    fixture.componentInstance.selectedOeId = 'oe-42';
    fixture.componentInstance.submitMapping();

    expect(emitted).toEqual(['oe-42']);
  });

  it('does not emit without a selection', () => {
    make();
    const fixture = TestBed.createComponent(FindingsDetailPanelComponent);
    fixture.componentRef.setInput('detail', detailOf({}));
    const emitted: string[] = [];
    fixture.componentInstance.complianceContextMapped.subscribe((id) => emitted.push(id));

    fixture.componentInstance.submitMapping();

    expect(emitted).toEqual([]);
  });
});
