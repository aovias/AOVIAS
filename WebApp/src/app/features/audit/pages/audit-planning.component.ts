import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { injectAuditWorkspace } from '../workspace/services/audit-workspace-participation.service';
import { AuditWorkspaceStateService } from '../workspace/services/audit-workspace-state.service';
import { ComplianceContextService } from '../../../core/services/compliance-context.service';
import { PlatformConfigurationService } from '../../../core/configuration/platform-configuration.service';
import { CONFIGURATION_CATEGORY } from '../../../core/configuration/audit-configuration.catalogs';
import {
  AuditManagementApiService,
  type AuditType,
  type ScheduledAuditDto,
} from '../services/audit-management-api.service';
import { AuditorApiService } from '../../auditor/services/auditor-api.service';
import {
  AuditPrepTemplateService,
  type AuditPrepTemplate,
} from '../services/audit-prep-template.service';

type ViewMode = 'list' | 'calendar' | 'annual' | 'templates';

@Component({
  selector: 'app-audit-planning',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="planning-page">
      <header class="page-header">
        <div>
          <h1>Audit calendar & planning</h1>
          <p class="subtitle">
            Annual plan, schedule, calendar, prep templates, and session handoff.
          </p>
        </div>
      </header>

      <nav class="view-tabs">
        <button type="button" [class.active]="view() === 'list'" (click)="setView('list')">
          Schedule list
        </button>
        <button type="button" [class.active]="view() === 'calendar'" (click)="setView('calendar')">
          Calendar
        </button>
        <button type="button" [class.active]="view() === 'annual'" (click)="setView('annual')">
          Annual plan
        </button>
        <button
          type="button"
          [class.active]="view() === 'templates'"
          (click)="setView('templates')"
        >
          Templates
        </button>
      </nav>

      <section class="panel create-panel">
        <h2>Schedule audit</h2>
        <div class="form-grid">
          <label>Title <input [(ngModel)]="title" placeholder="Q3 Internal audit" /></label>
          <label>Date <input type="date" [(ngModel)]="scheduledDate" /></label>
          <label
            >Type
            <!--
              Driven by Administration configuration, not a literal list. Adding an audit type is a
              configuration change; this markup does not move.
            -->
            <select [(ngModel)]="auditType">
              @for (type of schedulableAuditTypes(); track type.id) {
                <option [value]="type.id">{{ type.label }}</option>
              }
            </select>
          </label>
          <label>Program year <input type="number" [(ngModel)]="programYear" /></label>
          <label
            >Program month <input type="number" min="1" max="12" [(ngModel)]="programMonth"
          /></label>
          <label
            >Program name <input [(ngModel)]="programName" placeholder="FY Internal Audit Plan"
          /></label>
          <label>Lead auditor <input [(ngModel)]="auditorName" placeholder="Lead auditor" /></label>
          <label
            >Team notes <input [(ngModel)]="teamNotes" placeholder="Co-auditors / departments"
          /></label>
          <label
            >Repeat
            <select [(ngModel)]="recurrence">
              @for (option of recurrenceOptions(); track option.value) {
                <option [value]="option.value">{{ option.label }}</option>
              }
            </select>
          </label>
          <label
            >Apply template
            <select [(ngModel)]="selectedTemplateId" (ngModelChange)="applyTemplate($event)">
              <option value="">—</option>
              @for (t of templates(); track t.id) {
                <option [value]="t.id">{{ t.name }}</option>
              }
            </select>
          </label>
        </div>
        <button type="button" class="btn-primary" (click)="create()" [disabled]="saving()">
          {{ saving() ? 'Saving…' : 'Create scheduled audit' }}
        </button>
        @if (createError()) {
          <p class="error">{{ createError() }}</p>
        }
        @if (createInfo()) {
          <p class="info">{{ createInfo() }}</p>
        }
      </section>

      @if (view() === 'list') {
        <section class="panel">
          <div class="list-header">
            <h2>Scheduled audits</h2>
            <div class="filters">
              <label>Year <input type="number" [(ngModel)]="filterYear" (change)="load()" /></label>
              <label
                >Month
                <input type="number" min="1" max="12" [(ngModel)]="filterMonth" (change)="load()"
              /></label>
              <button type="button" class="btn-secondary" (click)="load()">Refresh</button>
            </div>
          </div>
          @if (loading()) {
            <p class="muted">Loading…</p>
          }
          @if (error()) {
            <p class="error">{{ error() }}</p>
          }
          @if (startError()) {
            <p class="error">{{ startError() }}</p>
          }
          <table>
            <thead>
              <tr>
                <th>Date</th>
                <th>Title</th>
                <th>Type</th>
                <th>Program</th>
                <th>Status</th>
                <th>Auditor</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (a of items(); track a.id) {
                <tr>
                  <td>{{ a.scheduledDate | date: 'mediumDate' }}</td>
                  <td>{{ a.title }}</td>
                  <td>{{ a.auditTypeLabel || a.auditType || '—' }}</td>
                  <td>
                    {{ a.programYear }}/{{ a.programMonth }}
                    {{ a.programName ? '· ' + a.programName : '' }}
                  </td>
                  <td>{{ a.status }}</td>
                  <td>{{ a.auditorName || '—' }}</td>
                  <td class="actions">
                    <button
                      type="button"
                      class="link-btn"
                      (click)="startSession(a)"
                      [disabled]="startingId() === a.id"
                    >
                      {{ startLabel(a) }}
                    </button>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="7" class="muted">No scheduled audits for this period.</td>
                </tr>
              }
            </tbody>
          </table>
        </section>
      }

      @if (view() === 'calendar') {
        <section class="panel">
          <div class="list-header">
            <h2>{{ calendarTitle() }}</h2>
            <div class="filters">
              <button type="button" class="btn-secondary" (click)="shiftMonth(-1)">Prev</button>
              <button type="button" class="btn-secondary" (click)="shiftMonth(1)">Next</button>
            </div>
          </div>
          <div class="cal-grid">
            @for (d of weekdays; track d) {
              <div class="cal-head">{{ d }}</div>
            }
            @for (cell of calendarCells(); track cell.key) {
              <div class="cal-cell" [class.muted-cell]="!cell.inMonth">
                <div class="cal-day">{{ cell.day }}</div>
                @for (a of cell.items; track a.id) {
                  <button
                    type="button"
                    class="cal-event"
                    (click)="startSession(a)"
                    [title]="sessionIdFor(a.id) ? a.title + ' — resume audit' : a.title"
                  >
                    {{ a.title }}
                  </button>
                }
              </div>
            }
          </div>
        </section>
      }

      @if (view() === 'annual') {
        <section class="panel">
          <h2>Annual audit plan — {{ filterYear }}</h2>
          <p class="muted">
            Grouped by program name (ScheduledAudit program metadata — no separate program entity).
          </p>
          @for (g of annualGroups(); track g.key) {
            <div class="annual-group">
              <h3>
                {{ g.label }} <span class="muted">({{ g.items.length }})</span>
              </h3>
              <ul>
                @for (a of g.items; track a.id) {
                  <li>
                    <strong>{{ a.programMonth ?? '?' }}/{{ a.programYear }}</strong>
                    — {{ a.title }}
                    <span class="muted"
                      >· {{ a.auditTypeLabel || a.auditType || '—' }} · {{ a.status }}</span
                    >
                    <button type="button" class="link-btn" (click)="startSession(a)">
                      {{ sessionIdFor(a.id) ? 'Resume' : 'Start' }}
                    </button>
                  </li>
                }
              </ul>
            </div>
          } @empty {
            <p class="muted">No scheduled audits for {{ filterYear }}.</p>
          }
        </section>
      }

      @if (view() === 'templates') {
        <section class="panel">
          <h2>Prep templates</h2>
          <p class="muted">
            Reusable defaults for scope, type, and lead auditor. Stored locally until a Templates
            API ships.
          </p>
          <div class="form-grid">
            <label>Name <input [(ngModel)]="tplName" /></label>
            <label
              >Type
              <select [(ngModel)]="tplType">
                @for (type of schedulableAuditTypes(); track type.id) {
                  <option [value]="type.id">{{ type.label }}</option>
                }
              </select>
            </label>
            <label>Default scope <input [(ngModel)]="tplScope" /></label>
            <label>Program name <input [(ngModel)]="tplProgram" /></label>
            <label>Lead auditor <input [(ngModel)]="tplLead" /></label>
          </div>
          <button type="button" class="btn-primary" (click)="saveTemplate()">Save template</button>
          <ul class="tpl-list">
            @for (t of templates(); track t.id) {
              <li>
                <strong>{{ t.name }}</strong>
                <span class="muted">· {{ t.auditType }} · {{ t.defaultScope }}</span>
                <button type="button" class="link-btn" (click)="applyTemplate(t.id)">Use</button>
                <button type="button" class="link-btn danger" (click)="removeTemplate(t.id)">
                  Delete
                </button>
              </li>
            }
          </ul>
        </section>
      }
    </div>
  `,
  styles: [
    `
      .planning-page {
        padding: var(--page-padding-block-start) var(--page-padding-inline)
          var(--page-padding-block-end);
      }
      .page-header {
        display: flex;
        justify-content: space-between;
        gap: 16px;
        flex-wrap: wrap;
        margin-bottom: 16px;
      }
      h1 {
        margin: 0 0 8px;
        font-size: var(--font-size-page-title);
      }
      .subtitle,
      .muted {
        color: var(--text-secondary, #666);
        font-size: 14px;
        margin: 0;
      }
      .header-actions {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;
      }
      .view-tabs {
        display: flex;
        gap: 6px;
        margin-bottom: 16px;
        flex-wrap: wrap;
      }
      .view-tabs button {
        padding: 8px 12px;
        border-radius: 6px;
        border: 1px solid var(--border-color, #e5e7eb);
        background: var(--card-bg, #fff);
        cursor: pointer;
        font-size: 13px;
      }
      .view-tabs button.active {
        background: var(--color-primary);
        color: #fff;
        border-color: var(--color-primary);
      }
      .panel {
        background: var(--card-bg, #fff);
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 16px;
        margin-bottom: 20px;
      }
      .form-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 12px;
        margin-bottom: 12px;
      }
      label {
        display: flex;
        flex-direction: column;
        gap: 6px;
        font-size: 13px;
        color: var(--text-secondary, #555);
      }
      input,
      select {
        padding: 8px 10px;
        border: 1px solid var(--border-color, #ddd);
        border-radius: 6px;
        font-size: 14px;
        background: var(--card-bg, #fff);
        color: var(--text-primary);
      }
      .btn-primary,
      .btn-secondary {
        padding: 10px 16px;
        border-radius: 6px;
        border: none;
        cursor: pointer;
        text-decoration: none;
        font-size: 13px;
        display: inline-block;
      }
      .btn-primary {
        background: var(--primary-color, var(--color-primary));
        color: #fff;
      }
      .btn-secondary {
        background: transparent;
        border: 1px solid var(--border-color, #ddd);
        color: var(--text-primary);
      }
      .list-header {
        display: flex;
        justify-content: space-between;
        gap: 12px;
        flex-wrap: wrap;
        margin-bottom: 12px;
      }
      .filters {
        display: flex;
        gap: 8px;
        align-items: end;
        flex-wrap: wrap;
      }
      table {
        width: 100%;
        border-collapse: collapse;
        font-size: 14px;
      }
      th,
      td {
        text-align: left;
        padding: 10px 8px;
        border-bottom: 1px solid var(--border-color, #f0f0f0);
      }
      .actions {
        white-space: nowrap;
      }
      .link-btn {
        background: none;
        border: none;
        color: var(--color-primary);
        cursor: pointer;
        padding: 0;
        font-size: 13px;
      }
      .link-btn.danger {
        color: #b91c1c;
      }
      .error {
        color: #ef4444;
      }
      .info {
        color: #047857;
      }
      .cal-grid {
        display: grid;
        grid-template-columns: repeat(7, 1fr);
        gap: 4px;
      }
      .cal-head {
        font-size: 11px;
        text-transform: uppercase;
        color: var(--text-secondary);
        padding: 6px;
        text-align: center;
      }
      .cal-cell {
        min-height: 88px;
        border: 1px solid var(--border-color, #eee);
        border-radius: 6px;
        padding: 6px;
        background: var(--card-bg, #fff);
      }
      .muted-cell {
        opacity: 0.45;
      }
      .cal-day {
        font-size: 12px;
        font-weight: 600;
        margin-bottom: 4px;
      }
      .cal-event {
        display: block;
        width: 100%;
        text-align: left;
        font-size: 11px;
        margin-bottom: 2px;
        padding: 2px 4px;
        border-radius: 4px;
        border: none;
        cursor: pointer;
        background: color-mix(in srgb, var(--color-primary) 16%, transparent);
        color: var(--text-primary);
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
      .annual-group {
        margin-top: 16px;
      }
      .annual-group h3 {
        margin: 0 0 8px;
        font-size: 15px;
      }
      .annual-group ul {
        margin: 0;
        padding-left: 18px;
      }
      .annual-group li {
        margin-bottom: 6px;
      }
      .tpl-list {
        list-style: none;
        padding: 0;
        margin: 16px 0 0;
      }
      .tpl-list li {
        padding: 10px 0;
        border-bottom: 1px solid var(--border-color, #f0f0f0);
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        align-items: baseline;
      }
    `,
  ],
})
export class AuditPlanningComponent implements OnInit {
  private readonly configuration = inject(PlatformConfigurationService);

  /** Audit types a planner may schedule — Administration configuration, not a literal list. */
  protected readonly schedulableAuditTypes = this.configuration.schedulableAuditTypes;

  /** Repeat cadences offered when scheduling a programme. */
  protected readonly recurrenceOptions = this.configuration.options(
    CONFIGURATION_CATEGORY.auditRecurrence,
  );

  private readonly api = inject(AuditManagementApiService);
  private readonly auditorApi = inject(AuditorApiService);
  private readonly templatesSvc = inject(AuditPrepTemplateService);
  private readonly context = inject(ComplianceContextService);
  private readonly router = inject(Router);
  private readonly workspaceState = inject(AuditWorkspaceStateService);
  /** Cleared automatically on destroy — no ngOnDestroy needed. */
  private readonly workspace = injectAuditWorkspace();

  /**
   * Execution sessions already started from a scheduled audit, keyed by
   * scheduled audit id. Drives Start vs Resume so one scheduled audit maps to
   * one execution lifecycle (Step 2).
   */
  private readonly sessionsByScheduledAudit = signal<ReadonlyMap<string, string>>(new Map());

  readonly view = signal<ViewMode>('list');
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly startingId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly createError = signal<string | null>(null);
  readonly createInfo = signal<string | null>(null);
  readonly startError = signal<string | null>(null);
  readonly items = signal<ScheduledAuditDto[]>([]);
  readonly templates = signal<AuditPrepTemplate[]>([]);

  readonly weekdays = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

  title = '';
  scheduledDate = new Date().toISOString().slice(0, 10);
  auditType: AuditType = 'InternalAudit';
  programYear = new Date().getFullYear();
  programMonth = new Date().getMonth() + 1;
  programName = '';
  auditorName = '';
  teamNotes = '';
  /** A configured cadence code, not a closed union — cadences are configuration. */
  recurrence = 'none';
  selectedTemplateId = '';
  filterYear = new Date().getFullYear();
  filterMonth: number | null = new Date().getMonth() + 1;

  tplName = '';
  tplType: AuditType = 'InternalAudit';
  tplScope = '';
  tplProgram = '';
  tplLead = '';

  readonly calendarTitle = computed(() => {
    const m = this.filterMonth ?? new Date().getMonth() + 1;
    return new Date(this.filterYear, m - 1, 1).toLocaleString(undefined, {
      month: 'long',
      year: 'numeric',
    });
  });

  readonly calendarCells = computed(() => {
    const year = this.filterYear;
    const month = (this.filterMonth ?? new Date().getMonth() + 1) - 1;
    const first = new Date(year, month, 1);
    const startPad = first.getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const cells: Array<{ key: string; day: number; inMonth: boolean; items: ScheduledAuditDto[] }> =
      [];
    const byDay = new Map<string, ScheduledAuditDto[]>();
    for (const a of this.items()) {
      const d = new Date(a.scheduledDate);
      if (d.getFullYear() !== year || d.getMonth() !== month) continue;
      const key = d.toISOString().slice(0, 10);
      const list = byDay.get(key) ?? [];
      list.push(a);
      byDay.set(key, list);
    }
    for (let i = 0; i < startPad; i++) {
      const d = new Date(year, month, -startPad + i + 1);
      cells.push({ key: `p-${i}`, day: d.getDate(), inMonth: false, items: [] });
    }
    for (let day = 1; day <= daysInMonth; day++) {
      const key = new Date(year, month, day).toISOString().slice(0, 10);
      cells.push({ key, day, inMonth: true, items: byDay.get(key) ?? [] });
    }
    while (cells.length % 7 !== 0) {
      const n = cells.length;
      cells.push({ key: `n-${n}`, day: n - startPad - daysInMonth + 1, inMonth: false, items: [] });
    }
    return cells;
  });

  readonly annualGroups = computed(() => {
    const map = new Map<string, ScheduledAuditDto[]>();
    for (const a of this.items()) {
      const key = a.programName?.trim() || `Year ${a.programYear ?? this.filterYear}`;
      const list = map.get(key) ?? [];
      list.push(a);
      map.set(key, list);
    }
    return [...map.entries()]
      .map(([key, items]) => ({
        key,
        label: key,
        items: items.slice().sort((x, y) => (x.programMonth ?? 0) - (y.programMonth ?? 0)),
      }))
      .sort((a, b) => a.label.localeCompare(b.label));
  });

  ngOnInit(): void {
    this.templates.set(this.templatesSvc.list());
    this.load();
    this.loadSessionLinks();
    this.publishParticipation();
  }

  /**
   * Planning's workspace declaration.
   *
   * Re-published whenever the schedule or the session-link index resolves, so
   * counts and related records reflect loaded data rather than the empty state
   * at init. Toolbar entries are verbs only — switching between list, calendar
   * and annual stays in the page's own view tabs, since those are navigation.
   */
  private publishParticipation(): void {
    const all = this.items();
    const now = Date.now();
    const started = all.filter((a) => this.sessionIdFor(a.id)).length;
    const upcoming = all
      .filter((a) => new Date(a.scheduledDate).getTime() >= now)
      .sort((a, b) => a.scheduledDate.localeCompare(b.scheduledDate))
      .slice(0, 6);

    this.workspace.set({
      stage: 'planning',
      subject: { kind: 'planning', title: `Programme ${this.filterYear}` },
      actions: [
        {
          id: 'planning.schedule-audit',
          label: 'Schedule audit',
          variant: 'primary',
          run: () => this.setView('list'),
        },
        {
          id: 'planning.save-template',
          label: 'Save as template',
          run: () => this.setView('templates'),
        },
      ],
      workflowState: {
        stage: 'planning',
        statusLabel:
          all.length === 0 ? 'No audits scheduled' : `${started} of ${all.length} started`,
        completionCriteria: 'Every scheduled audit has an execution session',
      },
      relatedRecords: upcoming.map((a) => ({
        id: a.id,
        label: a.title,
        kind: 'Scheduled audit',
        status: this.sessionIdFor(a.id) ? 'In execution' : 'Not started',
      })),
      documents: this.templates().map((t) => ({
        id: t.id,
        title: t.name,
        kind: 'Prep template',
      })),
      aiContext: {
        framework: this.context.getFrameworkId() ?? null,
        facts: {
          scheduledCount: String(all.length),
          startedCount: String(started),
          programYear: String(this.filterYear),
        },
      },
    });
  }

  setView(mode: ViewMode): void {
    this.view.set(mode);
    if (mode === 'annual') this.filterMonth = null;
    else if (mode === 'calendar' && this.filterMonth == null) {
      this.filterMonth = new Date().getMonth() + 1;
    }
    if (mode !== 'templates') this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .getCalendar({
        year: this.filterYear || undefined,
        month: this.view() === 'annual' ? undefined : this.filterMonth || undefined,
      })
      .pipe(
        catchError(() => {
          this.error.set('Unable to load scheduled audits.');
          return of([] as ScheduledAuditDto[]);
        }),
        finalize(() => this.loading.set(false)),
      )
      .subscribe((list) => {
        const year = this.filterYear;
        this.items.set(
          this.view() === 'annual' ? list.filter((a) => (a.programYear ?? year) === year) : list,
        );
        this.publishParticipation();
      });
  }

  shiftMonth(delta: number): void {
    let m = (this.filterMonth ?? 1) + delta;
    let y = this.filterYear;
    if (m < 1) {
      m = 12;
      y -= 1;
    }
    if (m > 12) {
      m = 1;
      y += 1;
    }
    this.filterMonth = m;
    this.filterYear = y;
    this.load();
  }

  applyTemplate(id: string): void {
    this.selectedTemplateId = id;
    const t = this.templates().find((x) => x.id === id);
    if (!t) return;
    this.auditType = t.auditType;
    this.programName = t.defaultProgramName ?? this.programName;
    this.auditorName = t.leadAuditorName ?? this.auditorName;
    this.teamNotes = t.teamNotes ?? this.teamNotes;
    if (!this.title.trim()) this.title = t.name;
  }

  saveTemplate(): void {
    if (!this.tplName.trim() || !this.tplScope.trim()) return;
    this.templatesSvc.upsert({
      name: this.tplName,
      auditType: this.tplType,
      defaultScope: this.tplScope,
      defaultProgramName: this.tplProgram,
      leadAuditorName: this.tplLead,
    });
    this.templates.set(this.templatesSvc.list());
    this.tplName = '';
    this.tplScope = '';
    this.tplProgram = '';
    this.tplLead = '';
  }

  removeTemplate(id: string): void {
    this.templatesSvc.remove(id);
    this.templates.set(this.templatesSvc.list());
  }

  create(): void {
    const frameworkId = this.context.getFrameworkId();
    if (!frameworkId) {
      this.createError.set('Select a framework in context before scheduling.');
      return;
    }
    if (!this.title.trim()) {
      this.createError.set('Title is required.');
      return;
    }

    const dates = this.buildRecurrenceDates();
    this.saving.set(true);
    this.createError.set(null);
    this.createInfo.set(null);

    const run = (index: number): void => {
      if (index >= dates.length) {
        this.saving.set(false);
        this.createInfo.set(`Created ${dates.length} scheduled audit(s).`);
        this.title = '';
        this.programName = '';
        this.auditorName = '';
        this.teamNotes = '';
        this.recurrence = 'none';
        this.load();
        return;
      }
      const d = dates[index];
      this.api
        .createScheduled({
          frameworkId,
          title:
            dates.length > 1
              ? `${this.title.trim()} (${index + 1}/${dates.length})`
              : this.title.trim(),
          scheduledDate: d.toISOString(),
          auditType: this.auditType,
          programYear: d.getFullYear(),
          programMonth: d.getMonth() + 1,
          programName: this.programName.trim() || undefined,
          auditorName: this.auditorName.trim() || undefined,
        })
        .pipe(catchError(() => of(null)))
        .subscribe((created) => {
          if (!created && index === 0) {
            this.saving.set(false);
            this.createError.set('Failed to create scheduled audit.');
            return;
          }
          run(index + 1);
        });
    };
    run(0);
  }

  /** Existing execution session for a scheduled audit, if it has been started. */
  sessionIdFor(scheduledAuditId: string): string | null {
    return this.sessionsByScheduledAudit().get(scheduledAuditId) ?? null;
  }

  /** Label reflects whether this audit already has an execution lifecycle. */
  startLabel(a: ScheduledAuditDto): string {
    if (this.startingId() === a.id) return 'Starting…';
    return this.sessionIdFor(a.id) ? 'Resume audit' : 'Start audit';
  }

  /**
   * Planning → Execution handoff (Step 2).
   *
   * One scheduled audit maps to one execution session. If the audit was already
   * started we resume that session; only a genuinely unstarted audit creates
   * one. Without this the button minted a fresh session on every click, so a
   * scheduled audit could accumulate several parallel execution lifecycles and
   * findings would scatter across them.
   */
  startSession(a: ScheduledAuditDto): void {
    const existingSessionId = this.sessionIdFor(a.id);
    if (existingSessionId) {
      this.openSession(existingSessionId, a.id);
      return;
    }

    const frameworkId = a.frameworkId || this.context.getFrameworkId();
    if (!frameworkId) {
      this.startError.set('Framework context is required to start a session.');
      return;
    }
    this.startingId.set(a.id);
    this.startError.set(null);
    this.auditorApi
      .createSession({
        auditType: (a.auditType as AuditType) || 'InternalAudit',
        frameworkId,
        scope: a.title,
        leadAuditorName: a.auditorName || this.auditorName || undefined,
        plannedStartDate: a.scheduledDate,
        summary: this.teamNotes.trim() || undefined,
        scheduledAuditId: a.id,
      })
      .pipe(
        catchError(() => {
          this.startError.set('Failed to start audit session from schedule.');
          return of(null);
        }),
        finalize(() => this.startingId.set(null)),
      )
      .subscribe((session) => {
        if (!session) return;
        // Record the link immediately: the user may come back to planning
        // before the session list is refetched, and clicking again must resume
        // rather than create a second session.
        this.sessionsByScheduledAudit.update((map) => new Map(map).set(a.id, session.id));
        this.openSession(session.id, a.id);
      });
  }

  /** Carry the audit into workspace context, then hand over to execution. */
  private openSession(sessionId: string, scheduledAuditId: string): void {
    this.workspaceState.setActiveAudit(sessionId, scheduledAuditId);
    void this.router.navigate(['/app/audit/execution/sessions', sessionId]);
  }

  /**
   * Build the scheduled-audit → session index.
   *
   * Sessions carry the link (`scheduledAuditId`), scheduled audits do not, so
   * the index is built from the session side. Failure is non-fatal: an empty
   * index just means every audit offers "Start", which is the pre-Phase-3
   * behaviour rather than a broken page.
   */
  private loadSessionLinks(): void {
    this.auditorApi
      .getSessions()
      .pipe(catchError(() => of([])))
      .subscribe((sessions) => {
        const map = new Map<string, string>();
        for (const s of sessions) {
          if (s.scheduledAuditId) map.set(s.scheduledAuditId, s.id);
        }
        this.sessionsByScheduledAudit.set(map);
        // Coverage counts depend on these links, and the two loads race.
        this.publishParticipation();
      });
  }

  /**
   * Expand the chosen cadence into the dates to schedule.
   *
   * Both the count and the interval come from the cadence's configuration rather than from its
   * name, so "Monthly ×3" becoming "Monthly ×6" is a configuration edit. Defaults of one occurrence
   * and no interval mean an unrecognised cadence schedules a single audit instead of nothing.
   */
  private buildRecurrenceDates(): Date[] {
    const base = new Date(this.scheduledDate);
    const category = CONFIGURATION_CATEGORY.auditRecurrence;
    const count = this.configuration.attribute(category, this.recurrence, 'occurrences', 1);
    const monthStep = this.configuration.attribute(category, this.recurrence, 'intervalMonths', 0);
    if (count <= 1) return [base];
    const dates: Date[] = [];
    for (let i = 0; i < count; i++) {
      dates.push(new Date(base.getFullYear(), base.getMonth() + i * monthStep, base.getDate()));
    }
    return dates;
  }
}
