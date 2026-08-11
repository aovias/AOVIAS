import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EmploymentApiService } from '../services/employment-api.service';

@Component({
  selector: 'app-transfer-wizard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section class="wizard">
      <a [routerLink]="['/app/workforce', employmentId]">← Back</a>
      <h1>Transfer Employment</h1>
      <label>Transfer kind <input [(ngModel)]="transferKind" /></label>
      <label>Effective date <input type="date" [(ngModel)]="effectiveDate" /></label>
      <label>Notes <textarea [(ngModel)]="notes"></textarea></label>
      <button type="button" (click)="submit()">Submit transfer</button>
      @if (message()) { <p>{{ message() }}</p> }
    </section>
  `,
  styles: [`.wizard { padding:1.5rem; display:grid; gap:.75rem; max-width:28rem; }`],
})
export class TransferWizardComponent {
  private readonly api = inject(EmploymentApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  employmentId = this.route.snapshot.paramMap.get('id') ?? '';
  transferKind = 'Department';
  effectiveDate = new Date().toISOString().slice(0, 10);
  notes = '';
  message = signal('');

  submit(): void {
    this.api.transfer(this.employmentId, {
      transferKind: this.transferKind,
      effectiveDate: this.effectiveDate,
      notes: this.notes,
    }).subscribe({
      next: () => this.router.navigate(['/app/workforce', this.employmentId]),
      error: () => this.message.set('Transfer failed.'),
    });
  }
}
