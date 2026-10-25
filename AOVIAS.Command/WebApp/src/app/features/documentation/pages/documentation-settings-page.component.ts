import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-documentation-settings-page',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/documentation-workspace.layout.scss',
  template: `
    <div class="qdms-workspace">
      <header class="page-header">
        <div>
          <h1>Documentation Settings</h1>
          <p class="subtitle">Approval chains, review frequencies, RBAC roles, and Copilot draft policies</p>
        </div>
      </header>

      <section class="panel">
        <h2>Approval Workflow</h2>
        <p class="muted">Default chain: Author → Department Reviewer → Quality Manager → Medical Superintendent</p>
        <p class="muted">Copilot may draft revisions — humans approve and publish. Copilot never publishes or approves.</p>
      </section>

      <section class="panel" style="margin-top:12px;">
        <h2>Integrations</h2>
        <ul class="muted">
          <li><a routerLink="/app/journey/execution">Journey</a> — mapped SOPs at OE execution</li>
          <li><a routerLink="/app/compliance-maturity">Compliance Maturity</a> — policy coverage</li>
          <li><a routerLink="/app/evidence/binder">Evidence</a> — reference controlled documents (policies ≠ evidence)</li>
          <li><a routerLink="/app/copilot">Compliance Copilot</a> — draft-only policy assistance</li>
          <li><a routerLink="/app/auditor/dashboard">Auditor Workspace</a> — read-only document review</li>
          <li><a routerLink="/app/analytics">Analytics Hub</a> — operational SSOT (no duplicated KPI engines)</li>
        </ul>
      </section>

      <section class="panel" style="margin-top:12px;">
        <h2>RBAC Roles</h2>
        <p class="muted">Author · Reviewer · Approver · Publisher · Reader · Auditor — every action audited.</p>
      </section>
    </div>
  `,
  styles: [`ul { padding-left: 20px; } li { margin-bottom: 6px; }`],
})
export class DocumentationSettingsPageComponent {}
