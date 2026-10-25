import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';
import { JourneyChapter } from '../../models/chapter.model';

@Component({
  selector: 'app-chapter-navigation-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside class="chapter-panel" aria-label="Chapters">
      <div class="nav-head">
        <h3>Chapters</h3>
        <input
          type="search"
          class="search"
          placeholder="Search chapters…"
          [ngModel]="store.chapterSearch()"
          (ngModelChange)="store.setChapterSearch($event)"
          aria-label="Filter chapters"
        />
        <button type="button" class="sort-btn" (click)="store.toggleChapterRiskSort()">
          {{ store.sortChaptersByRiskDesc() ? 'Sort: Risk' : 'Sort: Code' }}
        </button>
      </div>
      <div class="chapter-list" role="list">
        @for (ch of store.filteredChapters(); track trackById($index, ch)) {
          <div class="chapter-block" role="listitem">
            <button
              type="button"
              class="chapter-item"
              [attr.aria-current]="store.selectedChapterId() === ch.id ? 'true' : null"
              [class.active]="store.selectedChapterId() === ch.id"
              [attr.data-risk]="ch.risk"
              (click)="store.selectChapter(ch.id)"
            >
              <span class="risk-dot" [attr.data-risk]="ch.risk"></span>
              <div class="ch-body">
                <div class="ch-code">{{ ch.code }}</div>
                <div class="ch-name">{{ ch.name }}</div>
                <div class="ch-meta">
                  <span>Compliance {{ ch.complianceScorePercent }}%</span>
                  <span class="risk-pill" [attr.data-risk]="ch.risk">{{ riskLabel(ch.risk) }}</span>
                </div>
                <div class="ch-core">
                  Core:
                  {{
                    ch.coreComplianceStatus === 'met'
                      ? 'Met'
                      : ch.coreComplianceStatus === 'at_risk'
                        ? 'At risk'
                        : 'Failed'
                  }}
                  @if (ch.coreFailureFlag) {
                    <span class="badge core-fail">Core</span>
                  }
                  @if (ch.pendingAudit) {
                    <span class="badge pending">Audit</span>
                  }
                </div>
                <div class="ch-tasks">{{ ch.openIssuesCount }} open issues</div>
              </div>
            </button>
            <div class="standards-nested" role="group" [attr.aria-label]="'Standards in ' + ch.code">
              @for (st of ch.standards; track st.id) {
                <button
                  type="button"
                  class="std-btn"
                  [class.active]="store.selectedStandardId() === st.id"
                  (click)="store.selectStandard(st.id); $event.stopPropagation()"
                >
                  <span class="std-code">{{ st.code }}</span>
                  <span class="std-title">{{ st.title }}</span>
                  <span class="std-pct">{{ st.completionPercent }}%</span>
                </button>
              }
            </div>
          </div>
        }
      </div>
    </aside>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }
    .chapter-panel {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
      min-height: 0;
    }
    .nav-head h3 {
      margin: 0 0 var(--space-2);
      font-size: var(--font-size-body);
      font-weight: var(--font-weight-bold);
      color: var(--text-primary);
    }
    .search {
      width: 100%;
      padding: var(--space-2) 0.625rem;
      border-radius: var(--radius-md);
      border: var(--border-width-hairline) solid var(--border-color);
      font-size: var(--font-size-caption);
      background: var(--card-bg);
      color: var(--text-primary);
      min-height: var(--input-height);
    }
    .sort-btn {
      margin-top: var(--space-2);
      width: 100%;
      padding: 0.375rem;
      font-size: var(--font-size-caption);
      border-radius: var(--radius-sm);
      border: var(--border-width-hairline) solid var(--border-color);
      background: var(--bg-tertiary);
      cursor: pointer;
      font-family: inherit;
    }
    .chapter-block {
      border: var(--border-width-hairline) solid var(--border-color);
      border-radius: 0.625rem;
      overflow: hidden;
      background: var(--card-bg);
      box-shadow: var(--shadow-xs);
    }
    .chapter-list {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      overflow: auto;
      flex: 1;
      padding-right: var(--space-1);
    }
    .standards-nested {
      display: flex;
      flex-direction: column;
      border-top: var(--border-width-hairline) solid var(--border-color);
      background: var(--bg-tertiary);
    }
    .std-btn {
      display: grid;
      grid-template-columns: auto 1fr auto;
      gap: var(--space-2);
      align-items: center;
      text-align: left;
      padding: var(--space-2) var(--space-3);
      border: none;
      border-bottom: var(--border-width-hairline) solid var(--border-color);
      background: transparent;
      cursor: pointer;
      font-family: inherit;
      font-size: var(--font-size-caption);
      color: var(--text-primary);
    }
    .std-btn:last-child {
      border-bottom: none;
    }
    .std-btn:hover {
      background: color-mix(in srgb, var(--primary-color) 6%, transparent);
    }
    .std-btn.active {
      background: var(--primary-bg-light);
      font-weight: var(--font-weight-bold);
    }
    .std-code {
      font-weight: var(--font-weight-bold);
      color: var(--primary-color);
    }
    .std-title {
      color: var(--text-secondary);
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .std-pct {
      font-size: 0.6875rem;
      color: var(--text-secondary);
    }
    .chapter-item {
      display: flex;
      gap: var(--space-2);
      text-align: left;
      padding: var(--card-padding-compact);
      border: none;
      border-bottom: var(--border-width-hairline) solid transparent;
      width: 100%;
      background: transparent;
      cursor: pointer;
      font-family: inherit;
      color: inherit;
    }
    .chapter-item.active {
      background: color-mix(in srgb, var(--primary-color) 6%, transparent);
    }
    .risk-dot {
      width: 0.375rem;
      border-radius: 0.625rem;
      background: var(--border-color);
      flex-shrink: 0;
    }
    .risk-dot[data-risk='high'] {
      background: var(--danger-color);
    }
    .risk-dot[data-risk='medium'] {
      background: var(--warning-color);
    }
    .risk-dot[data-risk='low'] {
      background: var(--success-color);
    }
    .ch-code {
      font-size: var(--font-size-caption);
      font-weight: var(--font-weight-bold);
      color: var(--primary-color);
    }
    .ch-name {
      font-size: var(--font-size-caption);
      font-weight: var(--font-weight-semibold);
      color: var(--text-primary);
      line-height: 1.3;
    }
    .ch-meta {
      display: flex;
      justify-content: space-between;
      font-size: 0.6875rem;
      color: var(--text-secondary);
      margin-top: var(--space-1);
    }
    .risk-pill[data-risk='high'] {
      color: var(--danger-color);
    }
    .risk-pill[data-risk='medium'] {
      color: var(--warning-color);
    }
    .risk-pill[data-risk='low'] {
      color: var(--success-color);
    }
    .ch-core {
      font-size: 0.6875rem;
      margin-top: var(--space-2);
      padding-top: var(--space-2);
      border-top: var(--border-width-hairline) solid var(--border-color);
      display: flex;
      flex-wrap: wrap;
      gap: 0.375rem;
      align-items: center;
      color: var(--text-secondary);
    }
    .badge {
      font-size: 0.625rem;
      font-weight: var(--font-weight-bold);
      padding: 2px 0.375rem;
      border-radius: var(--radius-xs);
      text-transform: uppercase;
    }
    .badge.core-fail {
      background: color-mix(in srgb, var(--danger-color) 12%, transparent);
      color: var(--danger-color);
    }
    .badge.pending {
      background: color-mix(in srgb, var(--warning-color) 15%, transparent);
      color: var(--warning-color);
    }
    .ch-tasks {
      font-size: 0.6875rem;
      margin-top: var(--space-1);
      color: var(--text-secondary);
    }
  `,
})
export class ChapterNavigationPanelComponent {
  readonly store = inject(JourneyWorkspaceStore);

  trackById(_i: number, ch: JourneyChapter): string {
    return ch.id;
  }

  riskLabel(r: JourneyChapter['risk']): string {
    return r === 'high' ? 'High' : r === 'medium' ? 'Medium' : 'Low';
  }
}
