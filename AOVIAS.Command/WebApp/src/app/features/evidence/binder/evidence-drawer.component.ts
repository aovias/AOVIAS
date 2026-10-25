import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { EvidenceBinderStore } from './evidence-binder.store';
import { EvidenceListComponent } from './evidence-list.component';
import { EvidenceUploadComponent } from './evidence-upload.component';
import { ChecklistComponent } from './checklist.component';
import { AiMappingComponent } from './ai-mapping.component';
import { HistoryComponent } from './history.component';
import type { BinderTab } from './models';

@Component({
  selector: 'app-binder-evidence-drawer',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    EvidenceListComponent,
    EvidenceUploadComponent,
    ChecklistComponent,
    AiMappingComponent,
    HistoryComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (store.drawerOpen()) {
      <div class="backdrop" [class.fullscreen]="store.drawerMode() === 'fullscreen'" (click)="store.closeDrawer()"></div>
      <aside class="drawer" [class.fullscreen]="store.drawerMode() === 'fullscreen'">
        <div class="head">
          <div>
            <h3>{{ store.overview()?.code ?? store.selectedObjective()?.code }} — Evidence</h3>
            @if (store.overview(); as o) {
              <p class="sub">{{ o.title }}</p>
            }
          </div>
          <button mat-icon-button type="button" class="close" aria-label="Close" (click)="store.closeDrawer()">✕</button>
        </div>
        <div class="tabs">
          @for (t of tabs; track t.id) {
            <button type="button" class="tab" [class.active]="store.activeTab() === t.id" (click)="setTab(t.id)">
              {{ t.label }}
            </button>
          }
        </div>
        <div class="body">
          @if (store.drawerPrefetchLoading()) {
            <mat-spinner diameter="32" />
          } @else if (store.activeTab() === 'evidence') {
            <app-binder-evidence-upload
              [uploading]="store.uploadLoading()"
              (filePicked)="store.uploadEvidenceFile($event)"
            />
            <app-binder-evidence-list
              [items]="store.evidences()"
              [loading]="store.evidencesLoading()"
              [page]="store.evidencesPage()"
              [pageSize]="store.evidencesPageSize()"
              [totalCount]="store.evidencesTotalCount()"
              [objectiveElementId]="store.selectedObjectiveElementId()"
              [objectiveElementCode]="store.overview()?.code ?? store.selectedObjective()?.code ?? null"
              (pageChange)="store.loadEvidencesPage($event)"
            />
          } @else if (store.activeTab() === 'checklist') {
            <app-binder-checklist [items]="store.checklist()" [loading]="store.checklistLoading()" />
          } @else if (store.activeTab() === 'ai-mapping') {
            <app-binder-ai-mapping [items]="store.aiMapping()" [loading]="store.aiMappingLoading()" />
          } @else {
            <app-binder-history [events]="store.history()" [loading]="store.historyLoading()" />
          }
        </div>
      </aside>
    }
  `,
  styles: [
    `
      .backdrop {
        position: fixed;
        inset: 0;
        background: rgba(0, 0, 0, 0.2);
        z-index: 40;
      }
      .backdrop.fullscreen {
        background: rgba(0, 0, 0, 0.45);
      }
      .drawer {
        position: fixed;
        top: 0;
        right: 0;
        width: min(440px, 100%);
        height: 100vh;
        background: var(--card-bg, #fff);
        border-left: 1px solid var(--border-color, #e5e7eb);
        z-index: 50;
        display: flex;
        flex-direction: column;
        box-shadow: -4px 0 24px rgba(0, 0, 0, 0.08);
      }
      .drawer.fullscreen {
        width: 100%;
        max-width: none;
        border-left: none;
      }
      .head {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        padding: 16px 12px 12px 16px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
      }
      .head h3 {
        margin: 0;
        font-size: 15px;
      }
      .sub {
        margin: 4px 0 0;
        font-size: 12px;
        color: #6b7280;
      }
      .close {
        font-size: 18px;
        line-height: 1;
      }
      .tabs {
        display: flex;
        gap: 4px;
        padding: 0 12px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
        overflow-x: auto;
      }
      .tab {
        padding: 10px 12px;
        border: none;
        background: none;
        cursor: pointer;
        font-size: 12px;
        color: #6b7280;
        border-bottom: 2px solid transparent;
        white-space: nowrap;
      }
      .tab.active {
        color: var(--primary-color, var(--color-primary));
        border-bottom-color: var(--primary-color, var(--color-primary));
        font-weight: 600;
      }
      .body {
        padding: 16px;
        overflow-y: auto;
        flex: 1;
      }
    `,
  ],
})
export class EvidenceDrawerComponent {
  readonly store = inject(EvidenceBinderStore);

  readonly tabs: { id: BinderTab; label: string }[] = [
    { id: 'evidence', label: 'Evidence' },
    { id: 'checklist', label: 'Checklist' },
    { id: 'ai-mapping', label: 'AI Mapping' },
    { id: 'history', label: 'History' },
  ];

  setTab(tab: BinderTab): void {
    this.store.setTab(tab);
  }
}
