import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';

@Component({
  selector: 'app-quality-settings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="ps-page ps-page--settings">
      <header class="page-header"><h1>Quality Intelligence Settings</h1></header>
      <section class="card">
        <h3>Indicator Catalog</h3>
        <p class="muted">Default NABH-aligned indicators are seeded automatically per organization. Additional categories can be configured in future releases.</p>
        <h3>Framework Mapping</h3>
        <p class="muted">Indicators support NABH, JCI, DNV, and ISO framework references. Extensible for additional accreditation bodies.</p>
        <h3>Business Rules</h3>
        <ul class="muted">
          <li>Threshold breaches generate advisory recommendations only</li>
          <li>Benchmark and threshold history is immutable</li>
          <li>Trend calculations use the centralized trend engine</li>
        </ul>
      </section>
    </div>
  `,
})
export class QualitySettingsPageComponent implements OnInit {
  private readonly aiContext = inject(AiPageContextService);
  ngOnInit(): void { this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', label: 'Settings' }); }
}
