import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TrainingApiService, TrainingAttendance, TrainingSession } from '../services/training-api.service';

@Component({
  selector: 'app-training-sessions-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="training-page">
      <header>
        <h1>Training Session Calendar</h1>
        <a routerLink="/app/settings/teams/training">← Programs</a>
      </header>

      <form class="create" (ngSubmit)="createSession()">
        <input [(ngModel)]="programId" name="programId" placeholder="Program ID" required />
        <input [(ngModel)]="sessionTitle" name="sessionTitle" placeholder="Session title" required />
        <input type="datetime-local" [(ngModel)]="startsAtLocal" name="startsAtLocal" required />
        <input [(ngModel)]="location" name="location" placeholder="Location" />
        <button type="submit">Schedule session</button>
      </form>

      @if (loading()) { <p>Loading…</p> }
      @else {
        <table>
          <thead>
            <tr><th>When</th><th>Program</th><th>Session</th><th>Completion</th><th>WI</th><th></th></tr>
          </thead>
          <tbody>
            @for (s of sessions(); track s.id) {
              <tr>
                <td>{{ s.startsAt | date:'medium' }}</td>
                <td>{{ s.programTitle }}</td>
                <td>{{ s.title }}</td>
                <td>{{ s.completionPercent }}% ({{ s.attendedCount }}/{{ s.assignedCount }})</td>
                <td>
                  @if (s.controlledDocumentId) {
                    <a [routerLink]="['/app/documentation/sops']" [queryParams]="{ documentId: s.controlledDocumentId }">WI</a>
                  } @else { — }
                </td>
                <td><button type="button" (click)="openAttendance(s.id)">Attendance</button></td>
              </tr>
            }
          </tbody>
        </table>
      }

      @if (selectedSessionId()) {
        <section class="attendance">
          <h2>Attendance</h2>
          <form (ngSubmit)="assign()">
            <input [(ngModel)]="profileIdsCsv" name="profileIdsCsv" placeholder="Professional profile IDs (comma-separated)" />
            <button type="submit">Assign participants</button>
          </form>
          <ul>
            @for (a of attendances(); track a.id) {
              <li>
                {{ a.professionalProfileId }} — {{ a.attendanceStatus }}
                <button type="button" (click)="markAttended(a.id)">Mark attended</button>
                @if (a.certificateUrl) { <a [href]="a.certificateUrl" target="_blank" rel="noopener">Certificate</a> }
              </li>
            }
          </ul>
        </section>
      }
    </section>
  `,
  styles: [`
    .training-page { padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end); }
    header { display:flex; justify-content:space-between; align-items:center; }
    .create, .attendance form { display:flex; flex-wrap:wrap; gap:.5rem; margin:1rem 0; }
    table { width:100%; border-collapse:collapse; }
    th, td { padding:.5rem; border-bottom:1px solid #e5e7eb; text-align:left; }
    .attendance { margin-top:1.5rem; }
  `],
})
export class TrainingSessionsPageComponent implements OnInit {
  private readonly api = inject(TrainingApiService);
  private readonly route = inject(ActivatedRoute);

  sessions = signal<TrainingSession[]>([]);
  attendances = signal<TrainingAttendance[]>([]);
  selectedSessionId = signal<string | null>(null);
  loading = signal(false);

  programId = '';
  sessionTitle = '';
  startsAtLocal = '';
  location = '';
  profileIdsCsv = '';

  ngOnInit(): void {
    this.programId = this.route.snapshot.queryParamMap.get('programId') ?? '';
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
    this.api.listSessions(this.programId || undefined).subscribe({
      next: (rows) => { this.sessions.set(rows ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  createSession(): void {
    if (!this.programId.trim() || !this.sessionTitle.trim() || !this.startsAtLocal) return;
    this.api.createSession({
      trainingProgramId: this.programId.trim(),
      title: this.sessionTitle.trim(),
      startsAt: new Date(this.startsAtLocal).toISOString(),
      location: this.location.trim() || undefined,
    }).subscribe({
      next: () => {
        this.sessionTitle = '';
        this.location = '';
        this.reload();
      },
    });
  }

  openAttendance(sessionId: string): void {
    this.selectedSessionId.set(sessionId);
    this.api.listAttendances(sessionId).subscribe({
      next: (rows) => this.attendances.set(rows ?? []),
    });
  }

  assign(): void {
    const sessionId = this.selectedSessionId();
    if (!sessionId) return;
    const ids = this.profileIdsCsv.split(',').map((x) => x.trim()).filter(Boolean);
    if (!ids.length) return;
    this.api.assignParticipants(sessionId, ids).subscribe({
      next: (rows) => { this.attendances.set(rows ?? []); this.profileIdsCsv = ''; this.reload(); },
    });
  }

  markAttended(attendanceId: string): void {
    this.api.recordAttendance(attendanceId).subscribe({
      next: () => {
        const sessionId = this.selectedSessionId();
        if (sessionId) this.openAttendance(sessionId);
        this.reload();
      },
    });
  }
}
