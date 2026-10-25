import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  AgendaTemplate,
  CommitteeActionItem,
  CommitteeMeeting,
  GovernanceApiService,
  LookupValue,
  ManagementReviewAgenda,
} from '../services/governance-api.service';

@Component({
  selector: 'app-committee-meetings',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section>
      <header>
        <h1>Committee Meetings</h1>
        <a [routerLink]="['/app/governance/committees']">Back</a>
      </header>

      <form (ngSubmit)="schedule()" class="form-grid">
        <label>
          Meeting type
          <select [(ngModel)]="meetingTypeId" name="meetingTypeId" required>
            <option value="" disabled>Select type</option>
            @for (t of meetingTypes(); track t.id) {
              <option [value]="t.id">{{ t.name }}</option>
            }
          </select>
        </label>
        <label>
          Agenda template
          <select [(ngModel)]="agendaTemplateCode" name="agendaTemplateCode" (ngModelChange)="onTemplateChange($event)">
            <option value="">None</option>
            @for (t of templates(); track t.code) {
              <option [value]="t.code">{{ t.name }}</option>
            }
          </select>
        </label>
        <textarea [(ngModel)]="agenda" name="agenda" rows="4" placeholder="Agenda"></textarea>
        <button type="submit">Schedule meeting</button>
      </form>

      @if (mrAgenda()) {
        <aside class="mr">
          <h2>Management Review inputs</h2>
          <ul>
            @for (s of mrAgenda()!.agendaSections; track s) { <li>{{ s }}</li> }
          </ul>
          <p class="muted">{{ mrAgenda()!.notes }}</p>
        </aside>
      }

      @if (error()) { <p class="err">{{ error() }}</p> }

      <h2>Meetings</h2>
      <ul class="meetings">
        @for (m of meetings(); track m.id) {
          <li [class.active]="selectedId() === m.id">
            <button type="button" (click)="select(m)">{{ m.scheduledAt | date: 'medium' }} — {{ m.status }}</button>
            @if (m.minutesApproved) { <span class="badge">Minutes approved</span> }
          </li>
        }
      </ul>

      @if (selected(); as m) {
        <div class="detail">
          <h3>Meeting detail</h3>
          <p>Template: {{ m.agendaTemplateCode || '—' }}</p>
          <pre class="agenda">{{ m.agenda }}</pre>

          @if (m.status !== 'Completed') {
            <form (ngSubmit)="complete()" class="form-grid">
              <textarea [(ngModel)]="minutes" name="minutes" placeholder="Minutes" rows="3"></textarea>
              <input [(ngModel)]="attendance" name="attendance" placeholder="Attendance (comma-separated names)" />
              <button type="submit">Complete meeting</button>
            </form>
          } @else if (!m.minutesApproved) {
            <button type="button" (click)="approve()">Approve minutes</button>
          }

          <h4>Decisions</h4>
          <form (ngSubmit)="addDecision()" class="form-grid">
            <select [(ngModel)]="decisionTypeId" name="decisionTypeId" required>
              <option value="" disabled>Decision type</option>
              @for (t of decisionTypes(); track t.id) {
                <option [value]="t.id">{{ t.name }}</option>
              }
            </select>
            <input [(ngModel)]="decisionSummary" name="decisionSummary" placeholder="Summary" required />
            <input [(ngModel)]="linkDeficiencyId" name="linkDeficiencyId" placeholder="Deficiency ID (optional)" />
            <input [(ngModel)]="linkRiskId" name="linkRiskId" placeholder="Risk ID (optional)" />
            <input [(ngModel)]="linkTaskId" name="linkTaskId" placeholder="Task ID (optional)" />
            <button type="submit">Record decision</button>
          </form>
          <ul>
            @for (d of m.decisions; track d.id) {
              <li>{{ d.summary }}
                @if (d.deficiencyId) { <span class="link">Def {{ d.deficiencyId | slice:0:8 }}</span> }
                @if (d.riskId) { <span class="link">Risk {{ d.riskId | slice:0:8 }}</span> }
                @if (d.taskId) { <span class="link">Task {{ d.taskId | slice:0:8 }}</span> }
              </li>
            }
          </ul>

          <h4>Action tracking</h4>
          <form (ngSubmit)="addAction()" class="form-grid">
            <input [(ngModel)]="actionTitle" name="actionTitle" placeholder="Action title" required />
            <input [(ngModel)]="actionTaskId" name="actionTaskId" placeholder="Link existing Task ID (optional)" />
            <button type="submit">Add action item</button>
          </form>
          <ul>
            @for (a of m.actionItems; track a.id) {
              <li>
                <strong>{{ a.title }}</strong> — {{ a.status }}
                @if (a.taskId) { <span class="link">Task {{ a.taskId | slice:0:8 }}…</span> }
                @if (a.status === 'Open' || a.status === 'InProgress') {
                  <button type="button" (click)="completeAction(a)">Mark complete</button>
                }
              </li>
            }
          </ul>
        </div>
      }
    </section>
  `,
  styles: [`
    .form-grid { display: grid; gap: 0.5rem; max-width: 36rem; margin-bottom: 1rem; }
    select, input, textarea, button { font: inherit; }
    .meetings { list-style: none; padding: 0; }
    .meetings li { margin: 0.25rem 0; }
    .meetings li.active button { font-weight: 700; }
    .badge { margin-left: 0.5rem; font-size: 0.8rem; color: #067647; }
    .err { color: #b42318; }
    .muted { color: #667085; font-size: 0.9rem; }
    .mr { border-left: 3px solid var(--color-primary); padding-left: 0.75rem; margin-bottom: 1rem; }
    .agenda { white-space: pre-wrap; background: #f9fafb; padding: 0.5rem; }
    .link { margin-left: 0.35rem; font-size: 0.8rem; color: var(--color-primary-text); }
    .detail { margin-top: 1rem; }
  `],
})
export class CommitteeMeetingsComponent implements OnInit {
  private api = inject(GovernanceApiService);
  private route = inject(ActivatedRoute);

  committeeId = signal('');
  meetings = signal<CommitteeMeeting[]>([]);
  selectedId = signal('');
  selected = signal<CommitteeMeeting | null>(null);
  meetingTypes = signal<LookupValue[]>([]);
  decisionTypes = signal<LookupValue[]>([]);
  templates = signal<AgendaTemplate[]>([]);
  mrAgenda = signal<ManagementReviewAgenda | null>(null);
  error = signal('');

  meetingTypeId = '';
  agendaTemplateCode = '';
  agenda = '';
  minutes = '';
  attendance = '';
  decisionTypeId = '';
  decisionSummary = '';
  linkDeficiencyId = '';
  linkRiskId = '';
  linkTaskId = '';
  actionTitle = '';
  actionTaskId = '';

  ngOnInit(): void {
    this.route.paramMap.subscribe((p) => {
      const id = p.get('id') ?? '';
      this.committeeId.set(id);
      if (id) this.reload();
    });
    this.api.getLookups('MeetingType').subscribe({ next: (d) => this.meetingTypes.set(d) });
    this.api.getLookups('DecisionType').subscribe({ next: (d) => this.decisionTypes.set(d) });
    this.api.listAgendaTemplates().subscribe({ next: (d) => this.templates.set(d) });
  }

  onTemplateChange(code: string): void {
    const t = this.templates().find((x) => x.code === code);
    if (t?.sections?.length) {
      this.agenda = t.sections.map((s, i) => `${i + 1}. ${s}`).join('\n');
    }
    if (t?.isManagementReview) {
      this.api.getManagementReviewAgenda(this.committeeId()).subscribe({
        next: (d) => this.mrAgenda.set(d),
        error: () => this.mrAgenda.set(null),
      });
    } else {
      this.mrAgenda.set(null);
    }
  }

  reload(): void {
    const id = this.committeeId();
    this.api.listMeetings(id).subscribe({
      next: (rows) => {
        this.meetings.set(rows);
        const sel = this.selectedId();
        if (sel) {
          const found = rows.find((m) => m.id === sel) ?? null;
          this.selected.set(found);
        }
      },
      error: () => this.error.set('Failed to load meetings'),
    });
  }

  select(m: CommitteeMeeting): void {
    this.selectedId.set(m.id);
    this.selected.set(m);
    this.minutes = m.minutes ?? '';
  }

  schedule(): void {
    this.error.set('');
    this.api.scheduleMeeting(this.committeeId(), {
      scheduledAt: new Date().toISOString(),
      meetingTypeValueId: this.meetingTypeId,
      agenda: this.agenda,
      agendaTemplateCode: this.agendaTemplateCode || null,
    }).subscribe({
      next: (m) => {
        this.meetings.update((list) => [m, ...list]);
        this.select(m);
      },
      error: () => this.error.set('Schedule failed'),
    });
  }

  complete(): void {
    const m = this.selected();
    if (!m) return;
    const attendees = this.attendance
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean)
      .map((name) => ({ memberAssigneeId: '00000000-0000-0000-0000-000000000000', displayName: name, present: true }));
    this.api.completeMeeting(this.committeeId(), m.id, {
      minutes: this.minutes,
      attendanceJson: attendees.length ? JSON.stringify(attendees) : null,
    }).subscribe({
      next: () => this.reload(),
      error: () => this.error.set('Complete failed'),
    });
  }

  approve(): void {
    const m = this.selected();
    if (!m) return;
    this.api.approveMinutes(this.committeeId(), m.id).subscribe({
      next: () => this.reload(),
      error: () => this.error.set('Approve minutes failed'),
    });
  }

  addDecision(): void {
    const m = this.selected();
    if (!m) return;
    this.api.recordDecision(this.committeeId(), m.id, {
      decisionTypeValueId: this.decisionTypeId,
      summary: this.decisionSummary,
      deficiencyId: this.linkDeficiencyId || null,
      riskId: this.linkRiskId || null,
      taskId: this.linkTaskId || null,
    }).subscribe({
      next: () => {
        this.decisionSummary = '';
        this.reload();
      },
      error: () => this.error.set('Record decision failed'),
    });
  }

  addAction(): void {
    const m = this.selected();
    if (!m) return;
    this.api.createActionItem(this.committeeId(), m.id, {
      title: this.actionTitle,
      taskId: this.actionTaskId || null,
    }).subscribe({
      next: () => {
        this.actionTitle = '';
        this.actionTaskId = '';
        this.reload();
      },
      error: () => this.error.set('Create action failed'),
    });
  }

  completeAction(a: CommitteeActionItem): void {
    const m = this.selected();
    if (!m) return;
    this.api.updateActionStatus(this.committeeId(), m.id, a.id, 'Completed').subscribe({
      next: () => this.reload(),
      error: () => this.error.set('Update action failed'),
    });
  }
}
