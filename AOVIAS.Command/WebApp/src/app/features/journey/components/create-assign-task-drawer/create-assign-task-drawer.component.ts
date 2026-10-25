import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { A11yModule } from '@angular/cdk/a11y';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { SessionService } from '../../../../core/auth/session.service';
import { OrganizationMember } from '../../../../core/services/organization.service';
import { DepartmentLookup } from '../../../../shared/models/lookup.models';
import { CommonLookupService } from '../../../../shared/services/common-lookup.service';
import { JourneyWorkspaceStore } from '../../state/journey-workspace.store';
import { ComplianceTaskPriority, CreateTaskDraftVm } from '../../models/create-task-draft-vm.model';
import { ComplianceTaskApiService } from '../../services/compliance-task-api.service';
import { OrganizationMembersApiService } from '../../services/organization-members-api.service';
import { TaskDrawerSectionHeaderComponent } from './task-drawer-section-header.component';
import { AssigneeChipsFieldComponent } from './assignee-chips-field.component';

@Component({
  selector: 'app-create-assign-task-drawer',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    A11yModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatSnackBarModule,
    MatTooltipModule,
    TaskDrawerSectionHeaderComponent,
    AssigneeChipsFieldComponent,
  ],
  templateUrl: './create-assign-task-drawer.component.html',
  styleUrl: './create-assign-task-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateAssignTaskDrawerComponent implements OnInit {
  readonly store = inject(JourneyWorkspaceStore);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);
  private readonly session = inject(SessionService);
  private readonly orgMembersApi = inject(OrganizationMembersApiService);
  private readonly commonLookup = inject(CommonLookupService);
  private readonly complianceTaskApi = inject(ComplianceTaskApiService);

  readonly members = signal<OrganizationMember[]>([]);
  readonly membersLoading = signal(false);
  readonly departments = signal<DepartmentLookup[]>([]);
  readonly departmentsLoading = signal(false);
  readonly departmentsError = signal<string | null>(null);
  readonly createTaskSubmitting = signal(false);

  readonly taskForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
    assignedToUserId: [''],
    departmentId: [''],
    dueDate: new FormControl<Date | null>(null),
    effortEstimate: [''],
    priority: this.fb.nonNullable.control<ComplianceTaskPriority>(3),
  });

  private readonly syncDebounceMs = 400;

  /** Standard + OE UUIDs required by Compliance create API. */
  readonly linkageIdsReady = computed(() => {
    const d = this.store.taskAssignmentDraft();
    return !!(d.objectiveElementId && d.standardId);
  });

  /** Read-only linkage labels for pills (same source as draft sync). */
  readonly taskDraftVm = computed(() => this.store.taskAssignmentDraft());

  constructor() {
    this.taskForm.valueChanges
      .pipe(debounceTime(this.syncDebounceMs), takeUntilDestroyed())
      .subscribe(() => {
        if (!this.store.taskAssignmentDrawerOpen()) return;
        this.store.setTaskAssignmentDraft(this.mapFormToDraft());
      });
  }

  ngOnInit(): void {
    this.loadMembers();
    this.loadDepartments();
    this.patchFormFromDraft(this.store.taskAssignmentDraft());
  }

  private loadMembers(): void {
    const orgId = this.session.getOrganizationId();
    if (!orgId) {
      this.members.set([]);
      return;
    }
    this.membersLoading.set(true);
    this.orgMembersApi.getMembersForOrganization(orgId).subscribe({
      next: (m) => {
        this.members.set(m);
        this.membersLoading.set(false);
      },
      error: () => {
        this.members.set([]);
        this.membersLoading.set(false);
      },
    });
  }

  private loadDepartments(): void {
    this.departmentsLoading.set(true);
    this.departmentsError.set(null);
    this.commonLookup.loadDepartments(false).subscribe({
      next: (rows) => {
        this.departments.set(rows ?? []);
        this.departmentsLoading.set(false);
      },
      error: () => {
        this.departments.set([]);
        this.departmentsLoading.set(false);
        this.departmentsError.set('Could not load departments. Retry from task drawer later.');
      },
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.store.taskAssignmentDrawerOpen()) {
      this.onCancel();
    }
  }

  patchFormFromDraft(d: CreateTaskDraftVm): void {
    const legacy = d as CreateTaskDraftVm & { assigneeIds?: string[] };
    const assigned =
      d.assignedToUserId || (legacy.assigneeIds?.length ? legacy.assigneeIds[0] : '') || '';
    this.taskForm.patchValue(
      {
        title: d.title,
        description: d.description,
        assignedToUserId: assigned,
        departmentId: d.departmentId ?? '',
        dueDate: this.draftDueAtToDate(d.dueAt),
        effortEstimate: d.effortEstimate ?? '',
        priority: d.priority,
      },
      { emitEvent: false },
    );
  }

  mapFormToDraft(): CreateTaskDraftVm {
    const v = this.taskForm.getRawValue();
    const prev = this.store.taskAssignmentDraft();
    const deptName = this.resolveDepartmentName(v.departmentId);
    return {
      title: v.title,
      description: v.description,
      assignedToUserId: v.assignedToUserId,
      departmentId: v.departmentId ? v.departmentId : null,
      departmentLabel: deptName || prev.departmentLabel,
      dueAt: v.dueDate ? this.localDateToDraftIso(v.dueDate as Date) : null,
      effortEstimate: v.effortEstimate ?? '',
      priority: v.priority as ComplianceTaskPriority,
      standardId: prev.standardId,
      standardLabel: prev.standardLabel,
      objectiveElementId: prev.objectiveElementId,
      objectiveLabel: prev.objectiveLabel,
    };
  }

  private resolveDepartmentName(departmentId: string): string {
    if (!departmentId) return '';
    return this.departments().find((x) => x.id === departmentId)?.name ?? '';
  }

  /** Draft stores UTC-ish ISO with fixed noon; map to local calendar date for the picker. */
  private draftDueAtToDate(iso: string | null): Date | null {
    if (!iso) return null;
    const part = iso.slice(0, 10);
    const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(part);
    if (!m) return null;
    const y = +m[1];
    const mo = +m[2];
    const d = +m[3];
    return new Date(y, mo - 1, d);
  }

  /** Same string shape as legacy `yyyy-MM-dd` + `T12:00:00.000Z` for draft + API. */
  private localDateToDraftIso(value: Date): string {
    const y = value.getFullYear();
    const m = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}T12:00:00.000Z`;
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('task-drawer-backdrop')) {
      this.onCancel();
    }
  }

  onCancel(): void {
    this.store.closeTaskAssignmentDrawer();
    this.store.resetTaskAssignmentDraft();
  }

  onSaveDraft(): void {
    this.store.setTaskAssignmentDraft(this.mapFormToDraft());
    this.store.saveTaskAssignmentDraftToSession();
    this.snackBar.open('Draft saved', undefined, { duration: 3000 });
  }

  onCreateTask(): void {
    if (this.taskForm.invalid) {
      this.taskForm.markAllAsTouched();
      return;
    }
    const draft = this.mapFormToDraft();
    const oeId = draft.objectiveElementId;
    const standardId = draft.standardId;
    if (!oeId || !standardId) {
      this.snackBar.open(
        'Objective element and standard context are missing. Open this drawer from the OE wizard.',
        undefined,
        { duration: 6000 },
      );
      return;
    }
    if (!draft.departmentId?.trim()) {
      this.snackBar.open('Select a department.', undefined, { duration: 4000 });
      return;
    }
    if (!draft.assignedToUserId?.trim()) {
      this.snackBar.open('Select who this task is assigned to.', undefined, { duration: 4000 });
      return;
    }

    const v = this.taskForm.getRawValue();
    const body = {
      objectiveElementId: oeId,
      complianceStandardId: standardId,
      assignedToUserId: v.assignedToUserId.trim(),
      departmentId: v.departmentId.trim(),
      departmentName: this.resolveDepartmentName(v.departmentId) || undefined,
      dueDate: v.dueDate ? this.localDateToDraftIso(v.dueDate as Date) : undefined,
      title: v.title?.trim() || undefined,
      description: v.description?.trim() || undefined,
      priority: v.priority as ComplianceTaskPriority,
    };

    this.createTaskSubmitting.set(true);
    this.complianceTaskApi.createTask(body).subscribe({
      next: () => {
        this.createTaskSubmitting.set(false);
        this.snackBar.open('Task created', undefined, { duration: 4000 });
        this.store.closeTaskAssignmentDrawer();
        this.store.resetTaskAssignmentDraft();
      },
      error: (err: HttpErrorResponse) => {
        this.createTaskSubmitting.set(false);
        if (err.status === 409) {
          this.snackBar.open(
            'A compliance task already exists for this hospital and objective element.',
            undefined,
            { duration: 7000 },
          );
          return;
        }
        const msg =
          (err.error && typeof err.error === 'object' && 'message' in err.error
            ? String((err.error as { message?: string }).message)
            : null) ||
          err.message ||
          'Could not create task.';
        this.snackBar.open(msg, undefined, { duration: 6000 });
      },
    });
  }

  onAssigneeChange(userId: string): void {
    this.taskForm.controls.assignedToUserId.setValue(userId);
  }

  /** Styling for priority options (ComplianceTaskPriority 1–4). */
  priorityClass(p: ComplianceTaskPriority): string {
    return `task-drawer__priority task-drawer__priority--p${p}`;
  }

  /** Mat-select may coerce option values; keep form control as numeric enum. */
  comparePriority(a: ComplianceTaskPriority, b: ComplianceTaskPriority): boolean {
    return Number(a) === Number(b);
  }

  trackByDeptId(_i: number, d: DepartmentLookup): string {
    return d.id;
  }

  /** Normalizes occasional bad characters in upstream standard/OE labels (e.g. "de?nes"). */
  sanitizeLinkageLabel(value: string): string {
    if (!value) return value;
    return value.replace(/\bde\?nes\b/gi, 'defines').replace(/\uFFFD/g, '');
  }
}
