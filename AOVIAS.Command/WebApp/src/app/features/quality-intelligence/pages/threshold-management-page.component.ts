import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiPageContextService } from '../../../core/services/ai-page-context.service';
import { QualityApiService } from '../services/quality-api.service';
import type { ClinicalIndicatorListItem } from '../models/quality-indicator.models';
import { formatQualityLabel, thresholdClass } from '../models/quality-indicator.models';

@Component({
  selector: 'app-threshold-management-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: '../styles/quality-intelligence-workspace.layout.scss',
  template: `
    <div class="ps-page ps-page--medium">
      <header class="page-header"><h1>Threshold Management</h1><p class="subtitle">Green / Yellow / Red / Critical — audited, never overwritten</p></header>
      <label class="field"><span class="field__label">Select Indicator</span>
        <select [(ngModel)]="selectedId" (ngModelChange)="onSelect()">
          <option value="">Choose…</option>
          @for (i of items(); track i.id) { <option [value]="i.id">{{ i.indicatorCode }} — {{ i.name }}</option> }
        </select>
      </label>
      @if (selectedId) {
        <div class="card" style="margin-top:16px">
          <label class="field"><span class="field__label">Target</span><input type="number" [(ngModel)]="targetValue" /></label>
          <label class="field"><span class="field__label">Green</span><input type="number" [(ngModel)]="greenThreshold" /></label>
          <label class="field"><span class="field__label">Yellow (Warning)</span><input type="number" [(ngModel)]="yellowThreshold" /></label>
          <label class="field"><span class="field__label">Red</span><input type="number" [(ngModel)]="redThreshold" /></label>
          <label class="field"><span class="field__label">Critical</span><input type="number" [(ngModel)]="criticalThreshold" /></label>
          <label class="field"><span class="field__label">Reason (audit trail)</span><textarea [(ngModel)]="reason" rows="2"></textarea></label>
          <button type="button" class="btn-primary" (click)="save()" [disabled]="saving()">Save Thresholds</button>
          @if (message()) { <p class="linked">{{ message() }}</p> }
        </div>
      }
    </div>
  `,
})
export class ThresholdManagementPageComponent implements OnInit {
  private readonly api = inject(QualityApiService);
  private readonly aiContext = inject(AiPageContextService);
  readonly items = signal<ClinicalIndicatorListItem[]>([]);
  readonly saving = signal(false);
  readonly message = signal('');
  selectedId = '';
  targetValue?: number; greenThreshold?: number; yellowThreshold?: number; redThreshold?: number; criticalThreshold?: number;
  reason = '';

  ngOnInit(): void {
    this.aiContext.setPageContext({ pageContext: 'QualityIntelligence', label: 'Threshold Management' });
    this.api.getIndicators({ pageSize: 100 }).subscribe((l) => this.items.set(l.items));
  }

  onSelect(): void {
    const item = this.items().find((i) => i.id === this.selectedId);
    if (!item) return;
    this.targetValue = item.targetValue;
    this.api.getById(item.id).subscribe((d) => {
      this.greenThreshold = d.greenThreshold;
      this.yellowThreshold = d.yellowThreshold;
      this.redThreshold = d.redThreshold;
      this.criticalThreshold = d.criticalThreshold;
    });
  }

  save(): void {
    if (!this.selectedId) return;
    this.saving.set(true);
    this.api.patchThresholds({
      indicatorId: this.selectedId,
      targetValue: this.targetValue,
      greenThreshold: this.greenThreshold,
      yellowThreshold: this.yellowThreshold,
      redThreshold: this.redThreshold,
      criticalThreshold: this.criticalThreshold,
      reason: this.reason || 'Threshold update via Threshold Management',
    }).subscribe({
      next: () => { this.message.set('Thresholds saved. Previous configuration preserved in audit history.'); this.saving.set(false); },
      error: () => { this.message.set('Failed to save thresholds.'); this.saving.set(false); },
    });
  }
}
