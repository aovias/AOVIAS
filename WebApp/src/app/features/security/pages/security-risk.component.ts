import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { SecurityApiService, UserRiskScore } from '../services/security-api.service';

@Component({
  selector: 'app-security-risk',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page">
      <h1><a routerLink="..">Security</a> / Risk score</h1>
      @if (risk()) {
        <p>Level: <strong>{{ risk()!.level }}</strong> (score {{ risk()!.score }})</p>
        <p>Calculated: {{ risk()!.calculatedAt | date:'medium' }}</p>
        <pre>{{ factors() }}</pre>
      } @else {
        <p>No risk score available yet.</p>
      }
    </section>
  `,
  styles: [`.page { padding: 1.5rem; } pre { background: #f5f5f5; padding: 1rem; overflow: auto; }`],
})
export class SecurityRiskComponent implements OnInit {
  private api = inject(SecurityApiService);
  risk = signal<UserRiskScore | null>(null);

  factors(): string {
    const r = this.risk();
    if (!r) return '';
    try { return JSON.stringify(JSON.parse(r.factorsJson), null, 2); } catch { return r.factorsJson; }
  }

  ngOnInit(): void {
    this.api.getRisk().subscribe({ next: (r) => this.risk.set(r), error: () => {} });
  }
}
