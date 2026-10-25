import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Phase5AiApiService } from '../../aovias/services/phase5-ai-api.service';
import { GovernanceApiService, GovernanceAnalytics, GovernanceKpis } from '../services/governance-api.service';

@Component({
  selector: 'app-governance-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section>
      <header>
        <h1>Enterprise Governance</h1>
        <p>Responsibilities, committees, approvals, and escalation — independent of security roles.</p>
        <nav>
          <a routerLink="responsibilities">Responsibilities</a> |
          <a routerLink="committees">Committees</a> |
          <a routerLink="approvals">Approvals</a> |
          <a routerLink="escalations">Escalations</a> |
          <a routerLink="ownership">Ownership</a> |
          <a routerLink="timeline">Timeline</a> |
          <a routerLink="analytics">Analytics</a>
        </nav>
        <div class="ai-report">
          <button type="button" (click)="generateReport()" [disabled]="reportLoading()">
            {{ reportLoading() ? 'Generating…' : 'Generate AI executive briefing' }}
          </button>
          @if (reportTitle()) {
            <p class="report-note">Generated: {{ reportTitle() }} (markdown — PDF/Word deferred)</p>
          }
          @if (reportError()) {
            <p class="report-error">{{ reportError() }}</p>
          }
        </div>
      </header>

      @if (kpis()) {
        <ul class="stats">
          <li>Active committees: {{ kpis()!.activeCommittees }}</li>
          <li>Open action items: {{ kpis()!.openActionItems }}</li>
          <li>Meetings this month: {{ kpis()!.meetingsThisMonth }}</li>
          <li>Pending minutes approvals: {{ kpis()!.pendingMinutesApprovals }}</li>
          <li>Effective documents: {{ kpis()!.documentsEffective }}</li>
          <li>Training overdue: {{ kpis()!.trainingOverdue }}</li>
          <li>Open incidents: {{ kpis()!.openIncidents }}</li>
          <li>Open risks: {{ kpis()!.openRisks }}</li>
        </ul>
        @if (kpis()!.sourceNotes) {
          <p class="muted">{{ kpis()!.sourceNotes }}</p>
        }
      } @else if (analytics()) {
        <ul class="stats">
          <li>Active responsibilities: {{ analytics()!.activeResponsibilities }}</li>
          <li>Active committees: {{ analytics()!.activeCommittees }}</li>
          <li>Published approval chains: {{ analytics()!.publishedApprovalChains }}</li>
          <li>Active assignments: {{ analytics()!.activeAssignments }}</li>
        </ul>
      }
    </section>
  `,
  styles: [
    `.stats { display: grid; grid-template-columns: repeat(auto-fill, minmax(12rem, 1fr)); gap: 0.5rem; padding: 0; list-style: none; }
     .stats li { background: #f2f4f7; padding: 0.75rem; border-radius: 0.35rem; }
     .muted { color: #667085; font-size: 0.85rem; }
     .ai-report { margin-top: 0.75rem; }
     .ai-report button {
       padding: 0.45rem 0.85rem; border-radius: 0.35rem; border: 1px solid #d0d5dd; background: #fff; cursor: pointer; font-weight: 600;
     }
     .report-note { font-size: 0.85rem; color: #475467; }
     .report-error { font-size: 0.85rem; color: #b42318; }`,
  ],
})
export class GovernanceDashboardComponent implements OnInit {
  private api = inject(GovernanceApiService);
  private phase5Ai = inject(Phase5AiApiService);
  analytics = signal<GovernanceAnalytics | null>(null);
  kpis = signal<GovernanceKpis | null>(null);
  reportLoading = signal(false);
  reportTitle = signal<string | null>(null);
  reportError = signal<string | null>(null);

  ngOnInit(): void {
    this.api.getKpis().subscribe({
      next: (d) => this.kpis.set(d),
      error: () => {
        this.api.getAnalytics().subscribe({ next: (d) => this.analytics.set(d), error: () => {} });
      },
    });
  }

  generateReport(): void {
    this.reportLoading.set(true);
    this.reportError.set(null);
    this.phase5Ai.generateReport('ExecutiveBriefing', 'monthly').subscribe({
      next: (res) => {
        this.reportTitle.set(res.title || res.reportType);
        this.reportLoading.set(false);
        if (res.markdown && typeof navigator !== 'undefined' && navigator.clipboard?.writeText) {
          void navigator.clipboard.writeText(res.markdown);
        }
      },
      error: () => {
        this.reportError.set('AI report generation unavailable.');
        this.reportLoading.set(false);
      },
    });
  }
}
