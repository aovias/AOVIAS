import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';

@Component({
  selector: 'app-patient-safety-settings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/patient-safety-workspace.layout.scss',
  template: `
    <div class="ps-page ps-page--settings">
      <header class="page-header">
        <h1>Patient Safety Settings</h1>
      </header>
      <p class="subtitle">Incident categories, escalation policies, and role mappings reuse platform Assignment, SLA, and Escalation engines.</p>
      <ul>
        <li>Reporter, Investigator, Department Head, Quality Manager roles map to existing platform roles</li>
        <li>Critical incidents inherit SLA policies where configured</li>
        <li>Anonymous reporting supported at incident capture</li>
        <li>External Auditor access is read-only via Auditor workspace</li>
      </ul>
    </div>
  `,
})
export class PatientSafetySettingsPageComponent implements OnInit {
  private readonly aiContext = inject(AiPageContextService);
  ngOnInit(): void { this.aiContext.setPageContext({ pageContext: 'PatientSafety' }); }
}
