import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AccessGovernanceApiService, ApprovalAuthorityItem } from '../services/access-governance-api.service';

@Component({
  selector: 'app-approval-matrix',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Approval Matrix</h1><a routerLink="/app/access-governance">Back</a></header>
      <table><thead><tr><th>User</th><th>Type</th><th>Level</th></tr></thead>
        <tbody>@for (a of items(); track a.id) {
          <tr><td>{{ a.userId }}</td><td>{{ a.approvalType }}</td><td>{{ a.approvalLevelValueId }}</td></tr>
        }</tbody>
      </table>
    </section>
  `,
})
export class ApprovalMatrixComponent implements OnInit {
  private api = inject(AccessGovernanceApiService);
  items = signal<ApprovalAuthorityItem[]>([]);
  ngOnInit(): void { this.api.listApprovalAuthorities().subscribe((d) => this.items.set(d)); }
}
