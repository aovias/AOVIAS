import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { GovernanceApiService, ResponsibilityItem } from '../../governance/services/governance-api.service';

@Component({
  selector: 'app-responsibility-matrix',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Responsibility Matrix</h1><a routerLink="/app/access-governance">Back</a></header>
      <p>Governance accountability assignments (independent of security roles).</p>
      <table><thead><tr><th>Code</th><th>Type</th><th>Resource</th></tr></thead>
        <tbody>@for (r of items(); track r.id) {
          <tr><td>{{ r.responsibilityCode }}</td><td>{{ r.responsibilityTypeValueId }}</td><td>{{ r.governedResourceId || '—' }}</td></tr>
        }</tbody>
      </table>
    </section>
  `,
})
export class ResponsibilityMatrixComponent implements OnInit {
  private api = inject(GovernanceApiService);
  items = signal<ResponsibilityItem[]>([]);
  ngOnInit(): void {
    this.api.listResponsibilities().subscribe((page) => this.items.set(page.items));
  }
}
