import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { GovernanceApiService, GovernanceAnalytics } from '../services/governance-api.service';

@Component({
  selector: 'app-governance-analytics',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header><h1>Governance Analytics</h1><a routerLink="/app/governance">Back</a></header>
      @if (data()) {
        <ul>
          <li>Active responsibilities: {{ data()!.activeResponsibilities }}</li>
          <li>Active committees: {{ data()!.activeCommittees }}</li>
          <li>Published approval chains: {{ data()!.publishedApprovalChains }}</li>
          <li>Active assignments: {{ data()!.activeAssignments }}</li>
        </ul>
      }
    </section>
  `,
})
export class GovernanceAnalyticsComponent implements OnInit {
  private api = inject(GovernanceApiService);
  data = signal<GovernanceAnalytics | null>(null);

  ngOnInit(): void {
    this.api.getAnalytics().subscribe((d) => this.data.set(d));
  }
}
