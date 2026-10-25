import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import type { IdentityUserListItem } from '../../settings/identity-admin/models/identity-user-admin.model';
import type { OeOwnershipMatrixItem } from '../services/oe-ownership-api.service';
import type { FlatDepartment, OeActivityState, OeDisplayStatus } from './oe-ownership-matrix.utils';

@Component({
  selector: 'app-oe-ownership-matrix-row',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    ScrollingModule,
    MatAutocompleteModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  template: `
    <div class="oe-matrix__row" [class.oe-matrix__row--dirty]="dirty()" role="row">
      <div class="oe-matrix__td oe-matrix__td--check">
        <mat-checkbox
          [checked]="selected()"
          (change)="selectChange.emit($event.checked)"
          [attr.aria-label]="'Select ' + row().objectiveElementCode"
        />
      </div>
      <div class="oe-matrix__td oe-matrix__td--code">{{ row().objectiveElementCode }}</div>
      <div
        class="oe-matrix__td oe-matrix__td--title"
        [matTooltip]="row().objectiveElementTitle"
        matTooltipShowDelay="400"
      >
        {{ row().objectiveElementTitle }}
      </div>
      <div class="oe-matrix__td">
        <input
          class="aov-input oe-matrix__cell-input"
          [ngModel]="deptLabel()"
          (ngModelChange)="deptSearch.emit($event)"
          [matAutocomplete]="deptAuto"
          placeholder="Select department"
          aria-label="Assigned Department"
        />
        <mat-autocomplete
          #deptAuto="matAutocomplete"
          [displayWith]="displayDeptFn()"
          (optionSelected)="deptSelected.emit($event.option.value)"
        >
          <cdk-virtual-scroll-viewport class="oe-matrix__ac" [itemSize]="40" style="height: 240px">
            <mat-option *cdkVirtualFor="let d of deptOptions(); trackBy: trackDept" [value]="d">
              <span [innerHTML]="deptOptionHtml()(d)"></span>
            </mat-option>
          </cdk-virtual-scroll-viewport>
        </mat-autocomplete>
      </div>
      <div class="oe-matrix__td">
        <input
          class="aov-input oe-matrix__cell-input"
          [ngModel]="ownerLabel()"
          (ngModelChange)="ownerSearch.emit($event)"
          [matAutocomplete]="ownerAuto"
          placeholder="Select owner"
          aria-label="Owner"
        />
        <mat-autocomplete
          #ownerAuto="matAutocomplete"
          [displayWith]="displayOwnerFn()"
          (optionSelected)="ownerSelected.emit($event.option.value)"
        >
          <mat-option disabled>
            <button type="button" class="aov-btn aov-btn--text aov-btn--sm" (click)="toggleShowAll.emit(); $event.stopPropagation()">
              {{ showAllUsers() ? 'Prefer department users' : 'Show all users' }}
            </button>
          </mat-option>
          @for (u of ownerOptions(); track u.userId) {
            <mat-option [value]="u">
              <div class="oe-matrix__person">
                <span class="oe-matrix__person-name">{{ u.fullName }}</span>
                <span class="aov-text-caption">{{ u.designation || u.role || '—' }}</span>
                <span class="aov-text-caption">{{ personMeta()(u) }}</span>
              </div>
            </mat-option>
          }
        </mat-autocomplete>
      </div>
      <div class="oe-matrix__td oe-matrix__td--due">
        <input
          class="aov-input oe-matrix__cell-input"
          [matDatepicker]="duePicker"
          [ngModel]="dueDate()"
          (ngModelChange)="dueChange.emit($event)"
          [placeholder]="duePlaceholder()"
          aria-label="Due Date"
        />
        <mat-datepicker-toggle [for]="duePicker" />
        <mat-datepicker #duePicker />
        <button
          type="button"
          class="aov-btn aov-btn--icon aov-btn--sm"
          matTooltip="No Due Date"
          (click)="dueChange.emit(null)"
          aria-label="Clear due date"
        >
          ×
        </button>
      </div>
      <div class="oe-matrix__td">
        <span class="aov-badge" [class]="badgeClass()">{{ status() }}</span>
      </div>
      <div class="oe-matrix__td oe-matrix__td--activity">
        @switch (activity()) {
          @case ('saving') {
            <mat-spinner diameter="18" matTooltip="Saving" aria-label="Saving" />
          }
          @case ('saved') {
            <mat-icon class="oe-matrix__ok" [matTooltip]="savedTooltip()" aria-label="Saved">check_circle</mat-icon>
          }
          @case ('error') {
            <button
              type="button"
              class="aov-btn aov-btn--icon aov-btn--sm oe-matrix__fail"
              [matTooltip]="errorMessage() || 'Failed to save — Retry'"
              (click)="retry.emit()"
              aria-label="Retry failed save"
            >
              <mat-icon>warning</mat-icon>
            </button>
          }
          @default {
            <span class="aov-text-muted" aria-hidden="true">—</span>
          }
        }
      </div>
    </div>
  `,
  styles: [
    `
      :host {
        display: block;
      }
      .oe-matrix__row {
        display: grid;
        grid-template-columns: 2.5rem 5.5rem minmax(10rem, 1.4fr) minmax(9rem, 1fr) minmax(10rem, 1.1fr) 8.25rem 6.875rem 3.5rem;
        align-items: center;
        min-block-size: var(--control-height-md, 2.75rem);
        border-block-end: var(--border-width-hairline) solid var(--border-color);
        transition: background-color var(--motion-duration-fast, 0.2s) var(--motion-easing-standard, ease);
      }
      .oe-matrix__row:hover {
        background: var(--state-hover-bg);
      }
      .oe-matrix__row--dirty {
        background: var(--color-status-info-soft);
      }
      .oe-matrix__td {
        padding: var(--table-cell-padding-block, var(--space-2)) var(--table-cell-padding-inline, var(--space-3));
        min-inline-size: 0;
        font-size: var(--table-body-font-size, var(--font-size-body-sm));
        color: var(--text-primary);
      }
      .oe-matrix__td--code {
        font-weight: var(--font-weight-semibold);
        color: var(--text-secondary);
      }
      .oe-matrix__td--title {
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
        line-height: var(--line-height-normal);
      }
      .oe-matrix__cell-input {
        inline-size: 100%;
        block-size: var(--control-height-sm, 2rem);
        min-block-size: var(--control-height-sm, 2rem);
        padding-block: 0;
      }
      .oe-matrix__td--due {
        display: flex;
        align-items: center;
        gap: var(--space-1);
      }
      .oe-matrix__td--activity {
        display: flex;
        justify-content: center;
      }
      .oe-matrix__ok {
        color: var(--color-status-success-fg);
        font-size: 1.25rem;
        width: 1.25rem;
        height: 1.25rem;
      }
      .oe-matrix__fail {
        color: var(--color-status-warning-fg);
      }
      .oe-matrix__person {
        display: grid;
        line-height: var(--line-height-normal);
      }
      .oe-matrix__person-name {
        font-weight: var(--font-weight-semibold);
      }
      .oe-matrix__ac {
        inline-size: 100%;
      }
    `,
  ],
})
export class OeOwnershipMatrixRowComponent {
  readonly row = input.required<OeOwnershipMatrixItem>();
  readonly selected = input(false);
  readonly dirty = input(false);
  readonly activity = input<OeActivityState>('idle');
  readonly status = input<OeDisplayStatus>('Unassigned');
  readonly deptLabel = input('');
  readonly ownerLabel = input('');
  readonly dueDate = input<Date | null>(null);
  readonly duePlaceholder = input('No Due Date');
  readonly deptOptions = input<FlatDepartment[]>([]);
  readonly ownerOptions = input<IdentityUserListItem[]>([]);
  readonly showAllUsers = input(false);
  readonly savedTooltip = input('Saved');
  readonly errorMessage = input<string | undefined>(undefined);
  readonly displayDeptFn = input.required<(v: FlatDepartment | string | null) => string>();
  readonly displayOwnerFn = input.required<(v: IdentityUserListItem | string | null) => string>();
  readonly deptOptionHtml = input.required<(d: FlatDepartment) => string>();
  readonly personMeta = input.required<(u: IdentityUserListItem) => string>();

  readonly selectChange = output<boolean>();
  readonly deptSearch = output<string>();
  readonly deptSelected = output<FlatDepartment>();
  readonly ownerSearch = output<string>();
  readonly ownerSelected = output<IdentityUserListItem>();
  readonly toggleShowAll = output<void>();
  readonly dueChange = output<Date | null>();
  readonly retry = output<void>();

  trackDept = (_: number, d: FlatDepartment) => d.id;

  badgeClass(): string {
    switch (this.status()) {
      case 'Assigned':
        return 'aov-badge--success';
      case 'Due Soon':
        return 'aov-badge--warning';
      case 'Overdue':
        return 'aov-badge--danger';
      default:
        return '';
    }
  }
}
