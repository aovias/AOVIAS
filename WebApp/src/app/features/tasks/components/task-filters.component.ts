import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import type {
  DeficiencySeverityFilter,
  DeficiencyTypeFilter,
  MaturityStageFilter,
  ObligationEvidenceStatusFilter,
  ObligationVerificationStatusFilter,
  TaskCategory,
  TaskFilters,
  TaskKind,
} from '../models';
import type { TaskApiStatus } from '../task-risk.util';
import { CommonLookupService } from '../../../shared/services/common-lookup.service';
import type { DepartmentLookup } from '../../../shared/models/lookup.models';
import { OrganizationService } from '../../../core/services/organization.service';
import { EvidenceBinderApiService } from '../../evidence/binder/evidence-binder-api.service';
import type { EvidenceBinderChapter } from '../../evidence/binder/models';

@Component({
  selector: 'app-task-filters',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="filters">
      <div class="search-wrap">
        <span class="search-ic" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="7" />
            <path d="M21 21l-4.3-4.3" />
          </svg>
        </span>
        <input
          type="search"
          placeholder="Search tasks, OE codes, or standards…"
          [ngModel]="search()"
          (ngModelChange)="onSearch($event)"
        />
      </div>
      <div class="select-row">
        <select [ngModel]="status()" (ngModelChange)="onStatus($event)" aria-label="Status">
          <option [ngValue]="undefined">All statuses</option>
          <option value="NotStarted">Not started</option>
          <option value="InProgress">In progress</option>
          <option value="Blocked">Blocked</option>
          <option value="Completed">Completed</option>
        </select>
        <select [ngModel]="priority()" (ngModelChange)="onPriority($event)" aria-label="Priority">
          <option [ngValue]="undefined">All priorities</option>
          <option [ngValue]="1">Urgent</option>
          <option [ngValue]="2">High</option>
          <option [ngValue]="3">Medium</option>
          <option [ngValue]="4">Low</option>
        </select>
        <select [ngModel]="category()" (ngModelChange)="onCategory($event)" aria-label="Category">
          <option [ngValue]="undefined">All categories</option>
          <option value="Compliance">Compliance</option>
          <option value="CAPA">CAPA</option>
          <option value="Audit">Audit</option>
          <option value="Training">Training</option>
          <option value="DocumentReview">Document review</option>
          <option value="General">General</option>
        </select>
        <select [ngModel]="taskKind()" (ngModelChange)="onTaskKind($event)" aria-label="Task kind">
          <option [ngValue]="undefined">All kinds</option>
          <option value="Standard">Standard</option>
          <option value="Approval">Approval</option>
          <option value="Review">Review</option>
        </select>
        <select [ngModel]="riskLevel()" (ngModelChange)="onRisk($event)" aria-label="Risk level">
          <option [ngValue]="undefined">All risk levels</option>
          <option value="Low">Low</option>
          <option value="Medium">Medium</option>
          <option value="High">High</option>
          <option value="Critical">Critical</option>
        </select>
        <select
          class="select-wide"
          [ngModel]="departmentId()"
          (ngModelChange)="onDepartment($event)"
          aria-label="Department"
        >
          <option [ngValue]="undefined">All departments</option>
          @for (d of departments(); track d.id) {
            <option [ngValue]="d.id">{{ d.name }}</option>
          }
        </select>
        <select
          class="select-wide"
          [ngModel]="ownerId()"
          (ngModelChange)="onOwner($event)"
          aria-label="Assigned to"
        >
          <option [ngValue]="undefined">All assignees</option>
          @for (o of owners(); track o.userId) {
            <option [ngValue]="o.userId">{{ o.label }}</option>
          }
        </select>
        <select
          class="select-wide"
          [ngModel]="chapterId()"
          (ngModelChange)="onChapter($event)"
          aria-label="Chapter"
        >
          <option [ngValue]="undefined">All chapters</option>
          @for (ch of chapters(); track ch.chapterId) {
            <option [ngValue]="ch.chapterId">{{ ch.code }} — {{ ch.name }}</option>
          }
        </select>
        <select
          [ngModel]="deficiencySeverity()"
          (ngModelChange)="onDeficiencySeverity($event)"
          aria-label="Deficiency severity"
        >
          <option [ngValue]="undefined">All severities</option>
          <option value="Critical">Critical</option>
          <option value="Major">Major</option>
          <option value="Moderate">Moderate</option>
          <option value="Minor">Minor</option>
        </select>
        <select
          [ngModel]="deficiencyType()"
          (ngModelChange)="onDeficiencyType($event)"
          aria-label="Deficiency type"
        >
          <option [ngValue]="undefined">All deficiency types</option>
          <option value="Evidence">Evidence</option>
          <option value="Verification">Verification</option>
          <option value="Implementation">Implementation</option>
          <option value="Assessment">Assessment</option>
          <option value="Maturity">Maturity</option>
        </select>
        <select
          [ngModel]="maturityStage()"
          (ngModelChange)="onMaturityStage($event)"
          aria-label="Maturity stage"
        >
          <option [ngValue]="undefined">All stages</option>
          <option value="Core">Core</option>
          <option value="Commitment">Commitment</option>
          <option value="Achievement">Achievement</option>
          <option value="Excellence">Excellence</option>
        </select>
        <select
          [ngModel]="obligationEvidenceStatus()"
          (ngModelChange)="onObligationEvidenceStatus($event)"
          aria-label="Evidence status"
        >
          <option [ngValue]="undefined">All evidence status</option>
          <option value="Missing">Missing</option>
          <option value="Partial">Partial</option>
          <option value="Submitted">Submitted</option>
          <option value="Verified">Verified</option>
        </select>
        <select
          [ngModel]="obligationVerificationStatus()"
          (ngModelChange)="onObligationVerificationStatus($event)"
          aria-label="Verification status"
        >
          <option [ngValue]="undefined">All verification status</option>
          <option value="NotVerified">Not verified</option>
          <option value="Pending">Pending</option>
          <option value="Verified">Verified</option>
          <option value="Rejected">Rejected</option>
        </select>
      </div>
    </div>
  `,
  styles: [
    `
      .filters {
        display: flex;
        flex-direction: row;
        flex-wrap: wrap;
        align-items: stretch;
        gap: 0.5rem 0.75rem;
        margin-bottom: 1.1rem;
      }
      .search-wrap {
        position: relative;
        flex: 1 1 200px;
        min-width: min(100%, 180px);
      }
      .search-ic {
        position: absolute;
        left: 0.85rem;
        top: 50%;
        transform: translateY(-50%);
        width: 1.1rem;
        height: 1.1rem;
        color: var(--color-text-muted);
        pointer-events: none;
        display: grid;
        place-items: center;
      }
      .search-ic svg {
        width: 100%;
        height: 100%;
      }
      input[type='search'] {
        width: 100%;
        box-sizing: border-box;
        min-height: 2.45rem;
        padding: 0.45rem 0.85rem 0.45rem 2.55rem;
        background: var(--color-bg-surface);
        color: var(--color-text-primary);
        border: 1px solid var(--color-border-subtle);
        border-radius: 10px;
        font-size: 0.9rem;
      }
      input[type='search']::placeholder {
        color: var(--color-text-muted);
      }
      input[type='search']:focus {
        outline: none;
        border-color: color-mix(in srgb, var(--color-brand-primary) 45%, var(--color-border-subtle));
        box-shadow: var(--shadow-focus-ring-primary);
      }
      .select-row {
        display: flex;
        flex-wrap: wrap;
        align-items: stretch;
        gap: 0.5rem;
        flex: 0 1 auto;
      }
      select {
        flex: 0 0 auto;
        width: clamp(7.25rem, 14vw, 10.5rem);
        max-width: 10.5rem;
        min-width: 7.25rem;
        min-height: 2.45rem;
        padding: 0.45rem 1.75rem 0.45rem 0.65rem;
        background: var(--color-bg-surface);
        color: var(--color-text-primary);
        border: 1px solid var(--color-border-subtle);
        border-radius: 10px;
        font-size: 0.85rem;
        cursor: pointer;
      }
      select.select-wide {
        width: clamp(8rem, 16vw, 11.5rem);
        max-width: 11.5rem;
        min-width: 8rem;
      }
    `,
  ],
})
export class TaskFiltersComponent {
  readonly model = input.required<TaskFilters>();
  readonly frameworkId = input<string | null>(null);
  readonly filtersChange = output<TaskFilters>();

  private readonly destroyRef = inject(DestroyRef);
  private readonly lookups = inject(CommonLookupService);
  private readonly org = inject(OrganizationService);
  private readonly binderApi = inject(EvidenceBinderApiService);

  readonly search = signal('');
  readonly status = signal<TaskApiStatus | undefined>(undefined);
  readonly priority = signal<number | undefined>(undefined);
  readonly category = signal<TaskCategory | undefined>(undefined);
  readonly taskKind = signal<TaskKind | undefined>(undefined);
  readonly riskLevel = signal<TaskFilters['riskLevel']>(undefined);
  readonly departmentId = signal<string | undefined>(undefined);
  readonly ownerId = signal<string | undefined>(undefined);
  readonly chapterId = signal<string | undefined>(undefined);
  readonly deficiencySeverity = signal<DeficiencySeverityFilter | undefined>(undefined);
  readonly deficiencyType = signal<DeficiencyTypeFilter | undefined>(undefined);
  readonly maturityStage = signal<MaturityStageFilter | undefined>(undefined);
  readonly obligationEvidenceStatus = signal<ObligationEvidenceStatusFilter | undefined>(undefined);
  readonly obligationVerificationStatus = signal<ObligationVerificationStatusFilter | undefined>(
    undefined,
  );

  readonly departments = signal<DepartmentLookup[]>([]);
  readonly chapters = signal<EvidenceBinderChapter[]>([]);
  readonly owners = signal<{ userId: string; label: string }[]>([]);

  constructor() {
    this.lookups
      .loadDepartments()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of<DepartmentLookup[]>([])),
      )
      .subscribe((d) => this.departments.set(d));

    this.org
      .loadOrganizationDetails()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError(() => of(null)),
        map((o) => {
          const members = o?.members ?? [];
          const seen = new Set<string>();
          const rows: { userId: string; label: string }[] = [];
          for (const m of members) {
            const uid = m.userId?.trim();
            if (!uid || seen.has(uid)) continue;
            seen.add(uid);
            const name = [m.firstName, m.lastName].filter(Boolean).join(' ').trim();
            const label = name || m.email?.trim() || uid;
            rows.push({ userId: uid, label });
          }
          rows.sort((a, b) => a.label.localeCompare(b.label, undefined, { sensitivity: 'base' }));
          return rows;
        }),
      )
      .subscribe((rows) => this.owners.set(rows));

    toObservable(this.frameworkId)
      .pipe(
        switchMap((fid) => (fid ? this.binderApi.getChapters(fid) : of<EvidenceBinderChapter[]>([]))),
        catchError(() => of([])),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((c) => this.chapters.set(c ?? []));

    effect(() => {
      const m = this.model();
      this.search.set(m.search ?? '');
      this.status.set(m.status);
      this.category.set(m.category);
      this.taskKind.set(m.taskKind);
      if (m.quick === 'highPriority') {
        this.priority.set(2);
      } else {
        this.priority.set(m.priority);
      }
      if (m.quick === 'highRisk') {
        this.riskLevel.set('High');
      } else {
        this.riskLevel.set(m.riskLevel);
      }
      this.departmentId.set(m.departmentId);
      this.ownerId.set(m.ownerId);
      this.chapterId.set(m.chapterId);
      this.deficiencySeverity.set(m.deficiencySeverity);
      this.deficiencyType.set(m.deficiencyType);
      this.maturityStage.set(m.maturityStage);
      this.obligationEvidenceStatus.set(m.obligationEvidenceStatus);
      this.obligationVerificationStatus.set(m.obligationVerificationStatus);
    });
  }

  private emit(): void {
    const m = this.model();
    this.filtersChange.emit({
      ...m,
      search: this.search() || undefined,
      status: this.status(),
      priority: this.priority(),
      category: this.category(),
      taskKind: this.taskKind(),
      riskLevel: this.riskLevel(),
      departmentId: this.departmentId(),
      ownerId: this.ownerId(),
      chapterId: this.chapterId(),
      deficiencySeverity: this.deficiencySeverity(),
      deficiencyType: this.deficiencyType(),
      maturityStage: this.maturityStage(),
      obligationEvidenceStatus: this.obligationEvidenceStatus(),
      obligationVerificationStatus: this.obligationVerificationStatus(),
    });
  }

  onSearch(v: string): void {
    this.search.set(v);
    this.emit();
  }

  onStatus(v: TaskApiStatus | undefined): void {
    this.status.set(v);
    this.emit();
  }

  onPriority(v: number | undefined): void {
    this.priority.set(v);
    this.emit();
  }

  onCategory(v: TaskCategory | undefined): void {
    this.category.set(v);
    this.emit();
  }

  onTaskKind(v: TaskKind | undefined): void {
    this.taskKind.set(v);
    this.emit();
  }

  onRisk(v: TaskFilters['riskLevel']): void {
    this.riskLevel.set(v);
    this.emit();
  }

  onDepartment(v: string | undefined): void {
    this.departmentId.set(v);
    this.emit();
  }

  onOwner(v: string | undefined): void {
    this.ownerId.set(v);
    this.emit();
  }

  onChapter(v: string | undefined): void {
    this.chapterId.set(v);
    this.emit();
  }

  onDeficiencySeverity(v: DeficiencySeverityFilter | undefined): void {
    this.deficiencySeverity.set(v);
    this.emit();
  }

  onDeficiencyType(v: DeficiencyTypeFilter | undefined): void {
    this.deficiencyType.set(v);
    this.emit();
  }

  onMaturityStage(v: MaturityStageFilter | undefined): void {
    this.maturityStage.set(v);
    this.emit();
  }

  onObligationEvidenceStatus(v: ObligationEvidenceStatusFilter | undefined): void {
    this.obligationEvidenceStatus.set(v);
    this.emit();
  }

  onObligationVerificationStatus(v: ObligationVerificationStatusFilter | undefined): void {
    this.obligationVerificationStatus.set(v);
    this.emit();
  }
}
