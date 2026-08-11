import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TrainingApiService, TrainingMatrixItem } from '../services/training-api.service';

@Component({
  selector: 'app-training-matrix-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="training-page">
      <header>
        <h1>Training / Competency Matrix</h1>
        <a routerLink="/app/settings/teams/training">← Programs</a>
      </header>
      <p>Mandatory and role/department coverage. Link through to work instructions (controlled documents).</p>
      @if (loading()) { <p>Loading…</p> }
      @else {
        <table>
          <thead>
            <tr><th>Code</th><th>Program</th><th>Role</th><th>Mandatory</th><th>Completion</th><th>Work instruction</th></tr>
          </thead>
          <tbody>
            @for (m of matrix(); track m.programId) {
              <tr>
                <td>{{ m.programCode }}</td>
                <td>{{ m.programTitle }}</td>
                <td>{{ m.roleCode || '—' }}</td>
                <td>{{ m.isMandatory ? 'Yes' : 'No' }}</td>
                <td>{{ m.completionPercent }}% ({{ m.completedCount }}/{{ m.assignedCount }})</td>
                <td>
                  @if (m.controlledDocumentId) {
                    <a [routerLink]="['/app/documentation/sops']" [queryParams]="{ documentId: m.controlledDocumentId }">Open WI</a>
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
    header { display:flex; justify-content:space-between; align-items:center; }
    table { width:100%; border-collapse:collapse; margin-top:1rem; }
    th, td { padding:.5rem; border-bottom:1px solid #e5e7eb; text-align:left; }
  `],
})
export class TrainingMatrixPageComponent implements OnInit {
  private readonly api = inject(TrainingApiService);
  matrix = signal<TrainingMatrixItem[]>([]);
  loading = signal(false);

  ngOnInit(): void {
    this.loading.set(true);
    this.api.getMatrix().subscribe({
      next: (rows) => { this.matrix.set(rows ?? []); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
