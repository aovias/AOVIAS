import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AccessGovernanceApiService, DelegationItem } from '../services/access-governance-api.service';

@Component({
  selector: 'app-delegation-center',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Delegation Center</h1><a routerLink="/app/access-governance">Back</a></header>
      <table><thead><tr><th>From</th><th>To</th><th>Status</th><th>Effective</th></tr></thead>
        <tbody>@for (d of items(); track d.id) {
          <tr><td>{{ d.fromUserId }}</td><td>{{ d.toUserId }}</td><td>{{ d.status }}</td><td>{{ d.effectiveFrom | date }}</td></tr>
        }</tbody>
      </table>
    </section>
  `,
})
export class DelegationCenterComponent implements OnInit {
  private api = inject(AccessGovernanceApiService);
  items = signal<DelegationItem[]>([]);
  ngOnInit(): void { this.api.listDelegations().subscribe((d) => this.items.set(d)); }
}
