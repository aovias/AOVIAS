import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EmploymentApiService } from '../services/employment-api.service';

@Component({
  selector: 'app-promotion-wizard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="wizard">
      <a [routerLink]="['/app/workforce', employmentId]">← Back</a>
      <h1>Promote Employment</h1>
      <label>New position ID <input [(ngModel)]="newPositionId" /></label>
      <label>Effective date <input type="date" [(ngModel)]="effectiveDate" /></label>
      <label>Notes <textarea [(ngModel)]="notes"></textarea></label>
      <button type="button" (click)="submit()">Submit promotion</button>
      @if (message()) { <p>{{ message() }}</p> }
    </section>
  `,
  styles: [`.wizard { padding:1.5rem; display:grid; gap:.75rem; max-width:28rem; }`],
})
export class PromotionWizardComponent {
  private readonly api = inject(EmploymentApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  employmentId = this.route.snapshot.paramMap.get('id') ?? '';
  newPositionId = '';
  effectiveDate = new Date().toISOString().slice(0, 10);
  notes = '';
  message = signal('');

  submit(): void {
    this.api.promote(this.employmentId, {
      newPositionId: this.newPositionId,
      effectiveDate: this.effectiveDate,
      notes: this.notes,
    }).subscribe({
      next: () => this.router.navigate(['/app/workforce', this.employmentId]),
      error: () => this.message.set('Promotion failed.'),
    });
  }
}
