import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TrainingApiService, TrainingProgram } from '../services/training-api.service';

@Component({
  selector: 'app-training-programs-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="training-page">
      <header>
        <h1>Training Programs</h1>
        <nav>
          <a routerLink="/app/settings/teams/training/sessions">Session calendar</a>
          <a routerLink="/app/settings/teams/training/matrix">Competency matrix</a>
        </nav>
      </header>

      <form class="create" (ngSubmit)="create()">
        <input [(ngModel)]="code" name="code" placeholder="Code" required />
        <input [(ngModel)]="title" name="title" placeholder="Title" required />
        <input [(ngModel)]="organizationId" name="organizationId" placeholder="Organization ID" required />
        <input [(ngModel)]="controlledDocumentId" name="controlledDocumentId" placeholder="WI / Controlled Document ID (optional)" />
        <label><input type="checkbox" [(ngModel)]="isMandatory" name="isMandatory" /> Mandatory</label>
        <button type="submit">Create program</button>
      </form>

      @if (loading()) { <p>Loading…</p> }
      @else if (error()) { <p class="err">{{ error() }}</p> }
      @else {
        <table>
          <thead>
            <tr><th>Code</th><th>Title</th><th>Mandatory</th><th>Sessions</th><th>WI</th></tr>
          </thead>
          <tbody>
            @for (p of programs(); track p.id) {
              <tr>
                <td>{{ p.code }}</td>
                <td><a [routerLink]="['/app/settings/teams/training/sessions']" [queryParams]="{ programId: p.id }">{{ p.title }}</a></td>
                <td>{{ p.isMandatory ? 'Yes' : 'No' }}</td>
                <td>{{ p.sessionCount }}</td>
                <td>
                  @if (p.controlledDocumentId) {
                    <a [routerLink]="['/app/documentation/sops']" [queryParams]="{ documentId: p.controlledDocumentId }">Open WI</a>
                  } @else { — }
                </td>
              </tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
  styles: [`
    .training-page { padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end); }
    header { display: flex; justify-content: space-between; gap: 1rem; align-items: center; }
    nav a { margin-left: .75rem; }
    .create { display: flex; flex-wrap: wrap; gap: .5rem; margin: 1rem 0; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: .5rem; border-bottom: 1px solid #e5e7eb; text-align: left; }
    .err { color: #b91c1c; }
  `],
})
export class TrainingProgramsPageComponent implements OnInit {
  private readonly api = inject(TrainingApiService);
  programs = signal<TrainingProgram[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  code = '';
  title = '';
  organizationId = '';
  controlledDocumentId = '';
  isMandatory = true;

  ngOnInit(): void { this.reload(); }

  reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.listPrograms().subscribe({
      next: (rows) => { this.programs.set(rows ?? []); this.loading.set(false); },
      error: () => { this.error.set('Failed to load training programs.'); this.loading.set(false); },
    });
  }

  create(): void {
    if (!this.code.trim() || !this.title.trim() || !this.organizationId.trim()) return;
    this.api.createProgram({
      code: this.code.trim(),
      title: this.title.trim(),
      organizationId: this.organizationId.trim(),
      controlledDocumentId: this.controlledDocumentId.trim() || undefined,
      isMandatory: this.isMandatory,
    }).subscribe({
      next: () => {
        this.code = '';
        this.title = '';
        this.controlledDocumentId = '';
        this.reload();
      },
      error: () => this.error.set('Failed to create program.'),
    });
  }
}
