import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  viewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';
import { deriveOeColumns } from '../../utils/oe-derivation.util';
import { getCumulativeCompliancePercent } from '../../utils/journey-scoring.util';
import { OeEvidenceItem } from '../../models/objective-element.model';

@Component({
  selector: 'app-journey-tabs',
  standalone: true,
  imports: [CommonModule, MatTabsModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="journey-tabs-wrap" aria-label="Journey workspace tabs">
      <mat-tab-group mat-stretch-tabs="false" animationDuration="200ms">
        <mat-tab label="Tasks">
          <div class="tab-pad">
            @if (!store.selectedStandard()) {
              <p class="muted">Select a standard to view action plans.</p>
            } @else {
              <ul class="ap-list">
                @for (
                  row of tasksForSelectedStandard();
                  track row.plan.id + row.oeCode
                ) {
                  <li>
                    <strong>{{ row.oeCode }}</strong> — {{ row.plan.title }}
                    <span class="pill">{{ row.plan.status }}</span>
                    <span class="sub">Owner: {{ row.plan.ownerName }} · Due {{ row.plan.deadline }}</span>
                  </li>
                } @empty {
                  <li class="muted">No action plans for this standard.</li>
                }
              </ul>
            }
          </div>
        </mat-tab>
        <mat-tab label="Evidence">
          <div class="tab-pad">
            @if (!store.selectedOeId()) {
              <p class="muted">Open an objective element (OE card) to attach evidence to that OE.</p>
            } @else {
              <p class="hint">Upload evidence for the selected objective element (max 10 MB).</p>
              <button type="button" class="btn" (click)="openEvidencePicker()">Upload file</button>
              <input
                #evidenceInput
                type="file"
                class="sr-only"
                (change)="onFile($event)"
              />
              <ul class="ev-list">
                @for (ev of evidenceForSelectedOe(); track ev.id) {
                  <li>{{ ev.name }} — {{ ev.uploadedAt | date: 'medium' }}</li>
                } @empty {
                  <li class="muted">No files yet.</li>
                }
              </ul>
            }
          </div>
        </mat-tab>
        <mat-tab label="Timeline">
          <div class="tab-pad">
            <ul class="tl-list">
              @for (ev of timelineEvents(); track ev.id) {
                <li>
                  <span class="ts">{{ ev.timestamp | date: 'short' }}</span>
                  {{ ev.title }} — {{ ev.description }}
                </li>
              } @empty {
                <li class="muted">No events.</li>
              }
            </ul>
          </div>
        </mat-tab>
        <mat-tab label="Findings">
          <div class="tab-pad">
            <ul>
              @for (f of findings(); track f) {
                <li>{{ f }}</li>
              } @empty {
                <li class="muted">No findings for this standard.</li>
              }
            </ul>
          </div>
        </mat-tab>
        <mat-tab label="Analytics">
          <div class="tab-pad">
            @if (store.selectedStandard(); as st) {
              <p>
                Cumulative compliance (standard):
                <strong>{{ standardCompliancePct() | number: '1.0-1' }}%</strong>
              </p>
              <mat-progress-bar mode="determinate" [value]="standardCompliancePct()" />
              <p class="muted small">Core OEs: {{ coreCount() }} · Derived columns use NABH classification.</p>
            } @else {
              <p class="muted">Select a standard.</p>
            }
          </div>
        </mat-tab>
      </mat-tab-group>
    </section>
  `,
  styles: `
    .journey-tabs-wrap {
      margin-top: var(--space-4);
      border: var(--border-width-hairline) solid var(--border-color);
      border-radius: var(--radius-card);
      overflow: hidden;
      background: var(--card-bg);
      box-shadow: var(--shadow-sm);
    }
    .tab-pad {
      padding: var(--space-4);
      min-height: 7.5rem;
    }
    .muted {
      color: var(--text-secondary);
      font-size: var(--font-size-caption);
      line-height: var(--line-height-normal);
    }
    .hint {
      font-size: var(--font-size-caption);
      margin: 0 0 var(--space-2);
    }
    .btn {
      padding: var(--space-2) 0.875rem;
      border-radius: var(--radius-md);
      border: var(--border-width-hairline) solid var(--border-color);
      background: var(--primary-color);
      color: var(--color-text-inverse);
      font-weight: var(--font-weight-semibold);
      font-size: var(--font-size-caption);
      cursor: pointer;
      font-family: inherit;
      margin-bottom: var(--space-3);
      min-height: var(--button-height);
    }
    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      overflow: hidden;
      clip: rect(0, 0, 0, 0);
    }
    .ap-list,
    .ev-list,
    .tl-list {
      margin: 0;
      padding-left: 1.125rem;
      font-size: var(--font-size-caption);
      color: var(--text-primary);
    }
    .ap-list li {
      margin-bottom: 0.625rem;
    }
    .pill {
      display: inline-block;
      margin-left: var(--space-2);
      padding: 2px var(--space-2);
      border-radius: 999px;
      font-size: 0.6875rem;
      background: var(--primary-bg-light);
      color: var(--primary-color);
    }
    .sub {
      display: block;
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
      margin-top: var(--space-1);
    }
    .ts {
      display: inline-block;
      min-width: 8.75rem;
      color: var(--text-secondary);
      font-size: var(--font-size-caption);
    }
    .small {
      font-size: var(--font-size-caption);
      margin-top: var(--space-3);
    }
    mat-progress-bar {
      margin-top: var(--space-2);
      border-radius: var(--radius-xs);
    }
  `,
})
export class JourneyTabsComponent {
  readonly store = inject(JourneyWorkspaceStore);
  private readonly evidenceInputRef = viewChild<ElementRef<HTMLInputElement>>('evidenceInput');

  tasksForSelectedStandard(): { plan: import('../../models/objective-element.model').OeActionPlan; oeCode: string }[] {
    const st = this.store.selectedStandard();
    if (!st) return [];
    const out: { plan: import('../../models/objective-element.model').OeActionPlan; oeCode: string }[] = [];
    for (const oe of st.objectiveElements) {
      for (const p of oe.actionPlans) {
        out.push({ plan: p, oeCode: oe.code });
      }
    }
    return out;
  }

  evidenceForSelectedOe(): OeEvidenceItem[] {
    const id = this.store.selectedOeId();
    if (!id) return [];
    const f = this.store.findOe(id);
    return f?.oe.evidence ?? [];
  }

  timelineEvents(): import('../../models/objective-element.model').OeHistoryEvent[] {
    const st = this.store.selectedStandard();
    if (!st) return [];
    const out: import('../../models/objective-element.model').OeHistoryEvent[] = [];
    for (const oe of st.objectiveElements) {
      out.push(...oe.history);
    }
    return out.sort((a, b) => b.timestamp.localeCompare(a.timestamp));
  }

  findings(): string[] {
    const st = this.store.selectedStandard();
    if (!st) return [];
    const lines: string[] = [];
    for (const oe of st.objectiveElements) {
      if (oe.isNa) continue;
      if (oe.score <= 2) {
        lines.push(`Non-compliance: ${oe.code} (score ${oe.score}).`);
      } else if (oe.score === 3) {
        lines.push(`Observation: ${oe.code} partial compliance (score 3).`);
      }
      const d = deriveOeColumns(oe);
      if (d.isMandatory && oe.score < 4) {
        lines.push(`Risk: Core OE ${oe.code} below threshold.`);
      }
    }
    return lines;
  }

  standardCompliancePct(): number {
    const st = this.store.selectedStandard();
    if (!st) return 0;
    const active = st.objectiveElements.filter((o) => !o.isNa);
    return getCumulativeCompliancePercent(active);
  }

  coreCount(): number {
    const st = this.store.selectedStandard();
    if (!st) return 0;
    return st.objectiveElements.filter((o) => o.classification === 'core').length;
  }

  openEvidencePicker(): void {
    this.evidenceInputRef()?.nativeElement.click();
  }

  onFile(ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.store.uploadEvidenceFile(file);
  }
}
