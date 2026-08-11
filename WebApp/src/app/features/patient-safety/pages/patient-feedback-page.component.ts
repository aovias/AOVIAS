import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { IncidentApiService } from '../services/incident-api.service';
import { FEEDBACK_TYPES, formatIncidentLabel, type PatientFeedback } from '../models/incident.models';

@Component({
  selector: 'app-patient-feedback-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header">
        <div>
          <h1>Patient Feedback</h1>
          <p class="subtitle">Independent entity — may link to incidents when appropriate</p>
        </div>
        <button type="button" class="btn-primary" (click)="showForm.set(true)">Capture Feedback</button>
      </header>
      <div class="grid-wrap">
        <table class="data-table">
          <thead><tr><th>#</th><th>Type</th><th>Title</th><th>Department</th><th>Status</th><th>Received</th></tr></thead>
          <tbody>
            @for (f of feedback(); track f.id) {
              <tr>
                <td>{{ f.feedbackNumber }}</td>
                <td>{{ formatLabel(f.feedbackType) }}</td>
                <td>{{ f.title }}</td>
                <td>{{ f.department || '—' }}</td>
                <td>{{ f.status }}</td>
                <td>{{ f.receivedAt | date: 'mediumDate' }}</td>
              </tr>
            } @empty {
              <tr><td colspan="6" class="empty">No patient feedback recorded.</td></tr>
            }
          </tbody>
        </table>
      </div>
      @if (showForm()) {
        <div class="modal-backdrop" (click)="showForm.set(false)" role="dialog" aria-modal="true">
          <div class="modal" (click)="$event.stopPropagation()">
            <header class="modal__head">
              <h2>Capture Feedback</h2>
              <button type="button" class="modal__close" (click)="showForm.set(false)" aria-label="Close">×</button>
            </header>
            <div class="modal__body">
              <label class="field"><span class="field__label">Type</span><select [(ngModel)]="formType">@for (t of feedbackTypes; track t.value) { <option [value]="t.value">{{ t.label }}</option> }</select></label>
              <label class="field"><span class="field__label">Title</span><input [(ngModel)]="formTitle" placeholder="Feedback title" /></label>
              <label class="field"><span class="field__label">Description</span><textarea [(ngModel)]="formDescription" rows="3" placeholder="Describe the feedback…"></textarea></label>
            </div>
            <footer class="modal__foot">
              <button type="button" class="btn-secondary" (click)="showForm.set(false)">Cancel</button>
              <button type="button" class="btn-primary" (click)="submit()">Submit</button>
            </footer>
          </div>
        </div>
      }
    </div>
  `,
})
export class PatientFeedbackPageComponent implements OnInit {
  private readonly api = inject(IncidentApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly feedback = signal<PatientFeedback[]>([]);
  readonly showForm = signal(false);
  feedbackTypes = FEEDBACK_TYPES;
  formType = 'Feedback';
  formTitle = '';
  formDescription = '';
  readonly formatLabel = formatIncidentLabel;

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'PatientSafety' });
    this.reload();
  }

  reload(): void { this.api.getFeedback().subscribe((f) => this.feedback.set(f)); }

  submit(): void {
    this.api.createFeedback({ feedbackType: this.formType, title: this.formTitle, description: this.formDescription })
      .subscribe(() => { this.showForm.set(false); this.formTitle = ''; this.formDescription = ''; this.reload(); });
  }
}
