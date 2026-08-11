import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { GovernanceApiService, CommitteeItem, LookupValue } from '../services/governance-api.service';

@Component({
  selector: 'app-committee-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section>
      <header><h1>Committee Management</h1><a routerLink="/app/governance">Back</a></header>
      <form (ngSubmit)="create()" class="form-grid">
        <input [(ngModel)]="form.code" name="code" placeholder="Committee code" required />
        <input [(ngModel)]="form.name" name="name" placeholder="Name" required />
        <label>
          Committee type
          <select [(ngModel)]="form.typeId" name="typeId" required>
            <option value="" disabled>Select type</option>
            @for (t of types(); track t.id) {
              <option [value]="t.id">{{ t.name }}</option>
            }
          </select>
        </label>
        <button type="submit">Create committee</button>
      </form>
      @if (error()) { <p class="err">{{ error() }}</p> }
      <table>
        <thead><tr><th>Code</th><th>Name</th><th>Status</th><th></th></tr></thead>
        <tbody>
          @for (c of committees(); track c.id) {
            <tr>
              <td>{{ c.committeeCode }}</td>
              <td>{{ c.committeeName }}</td>
              <td>{{ c.status }}</td>
              <td>
                <a [routerLink]="['/app/governance/committees', c.id, 'members']">Members</a> |
                <a [routerLink]="['/app/governance/committees', c.id, 'meetings']">Meetings</a>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </section>
  `,
  styles: [
    `.form-grid { display: grid; gap: 0.5rem; max-width: 28rem; margin-bottom: 1rem; }
     .err { color: #b42318; } select { width: 100%; }`,
  ],
})
export class CommitteeManagementComponent implements OnInit {
  private api = inject(GovernanceApiService);
  committees = signal<CommitteeItem[]>([]);
  types = signal<LookupValue[]>([]);
  error = signal('');
  form = { code: '', name: '', typeId: '' };

  ngOnInit(): void {
    this.api.listCommittees().subscribe({ next: (d) => this.committees.set(d), error: () => this.error.set('Failed to load committees') });
    this.api.getLookups('CommitteeType').subscribe({
      next: (d) => this.types.set(d),
      error: () => this.error.set('Failed to load committee types — ensure governance lookups are seeded'),
    });
  }

  create(): void {
    this.error.set('');
    this.api.createCommittee({
      committeeCode: this.form.code,
      committeeName: this.form.name,
      committeeTypeValueId: this.form.typeId,
    }).subscribe({
      next: (c) => {
        this.committees.update((list) => [...list, c]);
        this.form = { code: '', name: '', typeId: this.form.typeId };
      },
      error: () => this.error.set('Create failed'),
    });
  }
}
