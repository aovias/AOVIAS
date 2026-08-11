import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';
import { OeCardComponent } from '../oe-card/oe-card.component';
import { ComplianceStandard } from '../../models/standard.model';

@Component({
  selector: 'app-journey-main-workspace',
  standalone: true,
  imports: [CommonModule, OeCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (!store.selectedChapter()) {
      <p class="empty">Select a chapter to view standards and objective elements.</p>
    } @else if (!store.selectedStandard()) {
      <p class="empty">Select a standard from the left panel.</p>
      } @else {
        @let st = store.selectedStandard()!;
        <div class="wk-head">
        <h2>{{ st.code }} — {{ st.title }}</h2>
        <p class="meta">{{ st.description }}</p>
        <p class="meta">
          Completion {{ st.completionPercent }}% ·
          {{ riskLabel(st.riskBadge) }} risk
        </p>
      </div>
      @if (!store.objectiveElementsForSelectedStandard().length) {
        <p class="empty">No objective elements for this standard in the current lifecycle filter.</p>
      } @else {
        <div class="oe-list">
          @for (oe of store.objectiveElementsForSelectedStandard(); track oe.id) {
            <app-oe-card
              [oe]="oe"
              [readOnly]="store.readOnly()"
              (viewTasks)="onOeAction(st, oe.id, 'tasks')"
              (uploadEvidence)="onOeAction(st, oe.id, 'evidence')"
              (openDetails)="store.selectOe(st.id, oe.id)"
            />
          }
        </div>
      }
    }
  `,
  styles: `
    :host {
      display: block;
    }
    .wk-head h2 {
      margin: 0 0 var(--space-2);
      font-size: var(--font-size-section-title);
      font-weight: var(--font-weight-bold);
      color: var(--text-primary);
    }
    .meta {
      margin: 0 0 var(--space-5);
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
    }
    .empty {
      padding: var(--space-6);
      color: var(--text-secondary);
      font-size: var(--font-size-body);
    }
    .oe-list {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
    }
  `,
})
export class JourneyMainWorkspaceComponent {
  readonly store = inject(JourneyWorkspaceStore);

  riskLabel(r: 'low' | 'medium' | 'high'): string {
    return r === 'high' ? 'High' : r === 'medium' ? 'Medium' : 'Low';
  }

  onOeAction(st: ComplianceStandard, oeId: string, _kind: 'tasks' | 'evidence'): void {
    void _kind;
    this.store.selectOe(st.id, oeId);
  }
}
