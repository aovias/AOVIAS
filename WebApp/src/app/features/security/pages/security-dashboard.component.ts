import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SecurityApiService, SecurityDashboard } from '../services/security-api.service';

@Component({
  selector: 'app-security-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page">
      <header>
        <h1>Enterprise Security</h1>
        <p>Session intelligence, device trust, risk scoring, and security telemetry.</p>
        <nav>
          <a routerLink="devices">Devices</a> |
          <a routerLink="sessions">Sessions</a> |
          <a routerLink="risk">Risk</a> |
          <a routerLink="timeline">Timeline</a> |
          <a routerLink="policies">Policies</a> |
          <a routerLink="analytics">Analytics</a> |
          <a routerLink="/app/account/security">Account password &amp; MFA</a>
        </nav>
      </header>
      @if (dashboard()) {
        <ul class="stats">
          <li>Active sessions: {{ dashboard()!.activeSessions }}</li>
          <li>Locked accounts: {{ dashboard()!.lockedAccounts }}</li>
          <li>High-risk users: {{ dashboard()!.highRiskUsers }}</li>
          <li>Failed logins (24h): {{ dashboard()!.failedLogins24h }}</li>
          <li>MFA enabled: {{ dashboard()!.mfaEnabledUsers }} / {{ dashboard()!.totalUsers }}</li>
          <li>Trusted devices: {{ dashboard()!.trustedDevices }}</li>
          <li>Open incidents: {{ dashboard()!.openIncidents }}</li>
        </ul>
      }
    </section>
  `,
  styles: [`.page { padding: 1.5rem; } .stats { list-style: none; padding: 0; } .stats li { padding: 0.35rem 0; } nav a { margin-right: 0.5rem; }`],
})
export class SecurityDashboardComponent implements OnInit {
  private api = inject(SecurityApiService);
  dashboard = signal<SecurityDashboard | null>(null);

  ngOnInit(): void {
    this.api.getDashboard().subscribe({ next: (d) => this.dashboard.set(d), error: () => {} });
  }
}
