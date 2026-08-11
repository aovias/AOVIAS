import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AccessGovernanceApiService, PolicyItem } from '../services/access-governance-api.service';

@Component({
  selector: 'app-policy-designer',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Policy Designer</h1><a routerLink="/app/access-governance">Back</a></header>
      <p>Published ABAC policies with condition rules.</p>
      <table><thead><tr><th>Code</th><th>Name</th><th>Status</th><th>Priority</th></tr></thead>
        <tbody>@for (p of items(); track p.id) {
          <tr><td>{{ p.policyCode }}</td><td>{{ p.policyName }}</td><td>{{ p.status }}</td><td>{{ p.priority }}</td></tr>
        }</tbody>
      </table>
    </section>
  `,
})
export class PolicyDesignerComponent implements OnInit {
  private api = inject(AccessGovernanceApiService);
  items = signal<PolicyItem[]>([]);
  ngOnInit(): void { this.api.listPolicies().subscribe((d) => this.items.set(d)); }
}
