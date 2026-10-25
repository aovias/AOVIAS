import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ObjectiveCardComponent } from './objective-card.component';
import type { EvidenceBinderChapter, EvidenceBinderObjectiveElement } from './models';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-binder-objective-list',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule, ObjectiveCardComponent, ScrollingModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="panel">
      @if (chapter()) {
        <div class="panel-head">
          <h2>{{ chapter()!.code }} — {{ chapter()!.name }}</h2>
          <span class="count">{{ objectives().length }} objectives</span>
        </div>
        @if (loading()) {
          <div class="list">
            <mat-spinner diameter="32" />
          </div>
        } @else if (useVirtualScroll()) {
          <cdk-virtual-scroll-viewport class="list list--virtual" [itemSize]="virtualRowSize" role="list">
            <app-binder-objective-card
              *cdkVirtualFor="let o of objectives(); trackBy: trackByOeId"
              [objective]="o"
              [selected]="selectedObjectiveId() === o.objectiveElementId"
              (pick)="pick.emit($event)"
            />
          </cdk-virtual-scroll-viewport>
        } @else {
          <div class="list">
            @for (o of objectives(); track o.objectiveElementId) {
              <app-binder-objective-card
                [objective]="o"
                [selected]="selectedObjectiveId() === o.objectiveElementId"
                (pick)="pick.emit($event)"
              />
            } @empty {
              <p class="empty">No objectives in this chapter.</p>
            }
          </div>
        }
      } @else {
        <div class="empty-state">
          <h3>Select a chapter</h3>
          <p>Choose a framework chapter to view objectives and evidence.</p>
        </div>
      }
    </div>
  `,
  styles: [
    `
      .panel {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 12px;
        min-height: 320px;
        display: flex;
        flex-direction: column;
      }
      .panel-head {
        padding: 16px 20px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
      .panel-head h2 {
        margin: 0;
        font-size: var(--font-size-section-title);
      }
      .count {
        font-size: 12px;
        color: #6b7280;
      }
      .list {
        padding: 16px;
      }
      .list--virtual {
        display: block;
        block-size: min(60vh, 28rem);
        padding: 16px;
      }
      .empty,
      .empty-state {
        padding: 48px 24px;
        text-align: center;
        color: #6b7280;
      }
    `,
  ],
})
export class ObjectiveListComponent {
  readonly chapter = input<EvidenceBinderChapter | null>(null);
  readonly objectives = input<EvidenceBinderObjectiveElement[]>([]);
  readonly selectedObjectiveId = input<string | null>(null);
  readonly loading = input(false);
  readonly pick = output<string>();

  readonly virtualRowSize = 96;
  private readonly virtualEnabled = environment.binderVirtualScrollObjectivesEnabled;

  readonly useVirtualScroll = computed(
    () => this.virtualEnabled && this.objectives().length > 40,
  );

  trackByOeId = (_: number, o: EvidenceBinderObjectiveElement) => o.objectiveElementId;
}
