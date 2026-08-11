import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { GovernanceApiService, ResponsibilityItem } from '../services/governance-api.service';

@Component({
  selector: 'app-governance-responsibility-matrix',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <section>
      <header><h1>Responsibility Matrix</h1><a routerLink="/app/governance">Back</a></header>
      <form (ngSubmit)="create()" class="form-grid">
        <input [(ngModel)]="form.code" name="code" placeholder="Code" required />
        <input [(ngModel)]="form.typeId" name="typeId" placeholder="Responsibility type ID" required />
        <input [(ngModel)]="form.resourceType" name="resourceType" placeholder="Resource type" required />
        <input [(ngModel)]="form.resourceId" name="resourceId" placeholder="Resource ID" />
        <label><input type="checkbox" [(ngModel)]="form.requiresPrimary" name="requiresPrimary" /> Primary required</label>
        <button type="submit">Create</button>
      </form>
      <table>
        <thead><tr><th>Code</th><th>Resource</th><th>Primary</th><th>Assign</th></tr></thead>
        <tbody>
          @for (r of items(); track r.id) {
            <tr>
              <td>{{ r.responsibilityCode }}</td>
              <td>{{ r.governedResourceType }}{{ r.governedResourceId ? ' / ' + r.governedResourceId : '' }}</td>
              <td>{{ r.requiresPrimaryOwner ? 'Yes' : 'No' }}</td>
              <td>
                <button type="button" (click)="assign(r)">Assign primary</button>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </section>
  `,
  styles: [`.form-grid { display: grid; gap: 0.5rem; max-width: 32rem; margin-bottom: 1rem; }`],
})
export class ResponsibilityMatrixComponent implements OnInit {
  private api = inject(GovernanceApiService);
  items = signal<ResponsibilityItem[]>([]);
  form = { code: '', typeId: '', resourceType: 'Department', resourceId: '', requiresPrimary: true };

  ngOnInit(): void {
    this.reload();
  }

  reload(): void {
    this.api.listResponsibilities().subscribe((p) => this.items.set(p.items));
  }

  create(): void {
    this.api.createResponsibility({
      responsibilityCode: this.form.code,
      responsibilityTypeValueId: this.form.typeId,
      governedResourceType: this.form.resourceType,
      governedResourceId: this.form.resourceId || null,
      requiresPrimaryOwner: this.form.requiresPrimary,
    }).subscribe(() => this.reload());
  }

  assign(r: ResponsibilityItem): void {
    const assigneeId = crypto.randomUUID();
    this.api.assignResponsibility(r.id, {
      assigneeType: 'Position',
      assigneeId,
      priority: 'Primary',
      effectiveFrom: new Date().toISOString(),
    }).subscribe(() => this.reload());
  }
}
