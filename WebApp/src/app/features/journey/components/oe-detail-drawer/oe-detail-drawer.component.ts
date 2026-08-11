import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  effect,
  inject,
  signal,
  viewChild,
  ElementRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';
import { NotificationService } from '../../../../core/ui/services/notification.service';
import { ActionPlanListComponent } from '../action-plan-list/action-plan-list.component';
import { JourneyEvidenceTabComponent } from '../journey-evidence-tab/journey-evidence-tab.component';
import { OeHistoryTimelineComponent } from '../oe-history-timeline/oe-history-timeline.component';
import { naRequiresJustification } from '../../utils/journey-validators';
import { OeEvidenceItem } from '../../models/objective-element.model';

@Component({
  selector: 'app-oe-detail-drawer',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ActionPlanListComponent,
    JourneyEvidenceTabComponent,
    OeHistoryTimelineComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <input
      #evFileInput
      type="file"
      class="sr-file"
      (change)="onEvidenceFile($event)"
      aria-hidden="true"
      tabindex="-1"
    />
    @if (store.drawerOpen()) {
      <div class="backdrop" (click)="close()" tabindex="-1" aria-hidden="true"></div>
      <aside
        class="drawer"
        role="dialog"
        aria-modal="true"
        [attr.aria-label]="drawerAriaLabel()"
      >
        @if (detail(); as d) {
          <header class="dh">
            <h3>{{ d.oe.code }}</h3>
            <button type="button" class="close" (click)="close()" aria-label="Close">×</button>
          </header>
          <p class="full-desc">{{ d.oe.description }}</p>
          <section>
            <h4>Score rationale</h4>
            <p>{{ d.oe.scoreRationale ?? '—' }}</p>
          </section>
          <section>
            <h4>Audit remarks</h4>
            <p>{{ d.oe.auditRemarks ?? '—' }}</p>
          </section>
          <section class="na-block" [formGroup]="naForm">
            <label class="na-row">
              <input
                type="checkbox"
                formControlName="isNa"
                [disabled]="store.readOnly() || store.isOeMutating(d.oe.id)"
              />
              Mark as not applicable (N/A)
            </label>
            <label class="sr-only" for="na-just">Justification</label>
            <textarea
              id="na-just"
              formControlName="naJustification"
              rows="3"
              placeholder="Required justification when N/A is selected"
              [disabled]="store.readOnly() || store.isOeMutating(d.oe.id)"
            ></textarea>
            @if (naError()) {
              <p class="err" role="alert" aria-live="polite">{{ naError() }}</p>
            }
            @if (!store.readOnly()) {
              <button
                type="button"
                class="save"
                (click)="saveNa(d)"
                [disabled]="store.isOeMutating(d.oe.id)"
              >
                Save
              </button>
            }
          </section>
          <section>
            <h4>Action plans</h4>
            <app-action-plan-list [plans]="d.oe.actionPlans" />
          </section>
          <section>
            <app-journey-evidence-tab
              [items]="d.oe.evidence"
              [readOnly]="store.readOnly()"
              (uploadRequested)="openEvidencePicker()"
            />
          </section>
          <section>
            <h4>History</h4>
            <app-oe-history-timeline [events]="d.oe.history" />
          </section>
        }
      </aside>
    }
  `,
  styles: `
    .sr-file {
      position: fixed;
      left: -9999px;
      width: 1px;
      height: 1px;
      opacity: 0;
    }
    .muted {
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
      margin: 0;
    }
    .backdrop {
      position: fixed;
      inset: 0;
      background: var(--overlay-backdrop);
      z-index: var(--z-modal);
    }
    .drawer {
      position: fixed;
      top: 0;
      right: 0;
      width: min(27.5rem, 100vw);
      height: 100vh;
      z-index: calc(var(--z-modal) + 1);
      background: var(--card-bg);
      border-left: var(--border-width-hairline) solid var(--border-color);
      padding: var(--space-4);
      overflow: auto;
      box-shadow: var(--shadow-elevated-panel);
    }
    .dh {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: var(--space-3);
    }
    .dh h3 {
      margin: 0;
      font-size: var(--font-size-heading-sm);
    }
    .close {
      border: none;
      background: transparent;
      font-size: var(--space-6);
      line-height: 1;
      cursor: pointer;
      color: var(--text-secondary);
    }
    .full-desc {
      font-size: var(--font-size-body);
      line-height: var(--line-height-normal);
      color: var(--text-secondary);
    }
    section {
      margin-top: var(--section-gap-y);
    }
    h4 {
      margin: 0 0 var(--space-2);
      font-size: var(--font-size-caption);
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--text-secondary);
    }
    .na-block textarea {
      width: 100%;
      margin-top: var(--space-2);
      padding: var(--space-2);
      border-radius: var(--radius-md);
      border: var(--border-width-hairline) solid var(--border-color);
      font-family: inherit;
      font-size: var(--font-size-caption);
      background: var(--card-bg);
      color: var(--text-primary);
      min-height: var(--input-height);
    }
    .na-row {
      display: flex;
      align-items: center;
      gap: var(--space-2);
      font-size: var(--font-size-caption);
      font-weight: var(--font-weight-semibold);
    }
    .save {
      margin-top: var(--space-2);
      padding: var(--space-2) var(--space-4);
      border-radius: var(--radius-md);
      border: none;
      background: var(--primary-color);
      color: var(--color-text-inverse);
      font-weight: var(--font-weight-semibold);
      cursor: pointer;
      min-height: var(--button-height);
    }
    .save:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
    .err {
      color: var(--danger-color);
      font-size: var(--font-size-caption);
      margin: var(--space-2) 0 0;
    }
    .sr-only {
      position: absolute;
      width: 1px;
      height: 1px;
      overflow: hidden;
    }
  `,
})
export class OeDetailDrawerComponent {
  readonly store = inject(JourneyWorkspaceStore);
  private readonly notification = inject(NotificationService);
  private readonly fb = inject(FormBuilder);

  private readonly evFileInput = viewChild<ElementRef<HTMLInputElement>>('evFileInput');

  drawerAriaLabel(): string {
    const d = this.detail();
    return d?.oe.code ? `Objective element ${d.oe.code}` : 'Objective element details';
  }

  readonly naForm = this.fb.group({
    isNa: [false],
    naJustification: [''],
  });

  readonly naError = signal<string | null>(null);

  readonly detail = signal<{
    chapterId: string;
    standardId: string;
    oe: import('../../models/objective-element.model').ObjectiveElement;
  } | null>(null);

  constructor() {
    effect(() => {
      this.store.snapshot();
      if (!this.store.drawerOpen()) return;
      const id = this.store.selectedOeId();
      if (!id) return;
      const found = this.store.findOe(id);
      if (!found) return;
      this.detail.set({
        chapterId: found.chapter.id,
        standardId: found.standard.id,
        oe: found.oe,
      });
      this.naForm.patchValue({
        isNa: found.oe.isNa,
        naJustification: found.oe.naJustification ?? '',
      });
      this.naError.set(null);
    });

  }

  close(): void {
    this.store.closeDrawer();
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.store.drawerOpen()) this.close();
  }

  saveNa(d: {
    chapterId: string;
    standardId: string;
    oe: import('../../models/objective-element.model').ObjectiveElement;
  }): void {
    const raw = this.naForm.getRawValue();
    const isNa = !!raw.isNa;
    const justification = (raw.naJustification ?? '').trim();
    const next = { ...d.oe, isNa, naJustification: justification || undefined };
    if (naRequiresJustification(next)) {
      this.naError.set('Justification is required when marking N/A.');
      return;
    }
    this.naError.set(null);
    this.store.applyLocalObjectiveElementPatch(d.chapterId, d.standardId, d.oe.id, {
      isNa,
      naJustification: justification || undefined,
    });
  }

  openEvidencePicker(): void {
    const oeId = this.store.selectedOeId();
    if (!oeId) {
      this.notification.showWarning('Select an objective element first.');
      return;
    }
    this.evFileInput()?.nativeElement.click();
  }

  onEvidenceFile(ev: Event): void {
    const oeId = this.store.selectedOeId();
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!oeId || !file) return;

    const item: OeEvidenceItem = {
      id: `ev-${Date.now()}`,
      name: file.name,
      verificationStatus: 'pending',
      uploadedAt: new Date().toISOString(),
      expiryAt: null,
      uploadedBy: 'Demo user',
    };
    this.store.appendLocalEvidence(oeId, item);
    this.notification.showSuccess('Evidence added (local demo).');
  }
}
