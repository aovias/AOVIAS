import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SecurityApiService, SecurityDashboard } from '../services/security-api.service';

@Component({
  selector: 'app-security-analytics',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page">
      <h1><a routerLink="..">Security</a> / Analytics</h1>
      @if (dashboard()) {
        <p>MFA adoption: {{ mfaPercent() }}%</p>
        <p>Incident rate indicator: {{ dashboard()!.openIncidents }} open / {{ dashboard()!.highRiskUsers }} high-risk users</p>
      }
    </section>
  `,
  styles: [`.page { padding: 1.5rem; }`],
})
export class SecurityAnalyticsComponent implements OnInit {
  private api = inject(SecurityApiService);
  dashboard = signal<SecurityDashboard | null>(null);

  mfaPercent(): number {
    const d = this.dashboard();
    if (!d || d.totalUsers === 0) return 0;
    return Math.round((d.mfaEnabledUsers / d.totalUsers) * 100);
  }

  ngOnInit(): void {
    this.api.getDashboard().subscribe({ next: (d) => this.dashboard.set(d), error: () => {} });
  }
}
