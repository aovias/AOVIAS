import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';

@Component({
  selector: 'app-traceability-hub-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/compliance-graph-workspace.layout.scss',
  template: `
    <div class="ps-page">
      <header class="page-header">
        <div>
          <h1>Compliance Traceability</h1>
          <p class="subtitle">Follow complete chains from objective elements through documentation, evidence, findings, CAPA, and clinical indicators</p>
        </div>
        <a routerLink="/app/compliance-graph/search" class="btn-secondary">Find an entity</a>
      </header>
      <section class="panel">
        <h2>How traceability works</h2>
        <p class="muted">Select any entity via Enterprise Search or the Graph Viewer, then open Entity Explorer for the full traceability chain, universal timeline, and cross-module navigation.</p>
        <ol>
          <li>Objective Element → Policy / SOP</li>
          <li>Evidence → Verification</li>
          <li>Finding → Deficiency → CAPA</li>
          <li>Incident → Investigation → Clinical Indicator</li>
          <li>Readiness Score → Audit Session → Executive Dashboard</li>
        </ol>
        <a routerLink="/app/auditor/assessment" class="btn-primary">Auditor Traceability (OE-centric)</a>
      </section>
    </div>
  `,
})
export class TraceabilityHubPageComponent implements OnInit {
  private readonly aiContext = inject(AiPageContextService);
  ngOnInit(): void { this.aiContext.setPageContext({ pageContext: 'ComplianceGraph', label: 'Traceability Hub' }); }
}
