import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatExpansionModule } from '@angular/material/expansion';
import { ComplianceStandard } from '../../models/standard.model';
import { ObjectiveElement } from '../../models/objective-element.model';

@Component({
  selector: 'app-standard-section',
  standalone: true,
  imports: [CommonModule, MatExpansionModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-expansion-panel class="std-panel" [expanded]="expanded()" (opened)="opened.emit()">
      <mat-expansion-panel-header>
        <mat-panel-title>
          <span class="std-code">{{ standard().code }}</span>
          <span class="std-risk" [attr.data-risk]="standard().riskBadge">{{ standard().riskBadge }}</span>
        </mat-panel-title>
        <mat-panel-description>
          <span class="pct">{{ standard().completionPercent }}%</span>
        </mat-panel-description>
      </mat-expansion-panel-header>
      <p class="std-desc">{{ standard().description }}</p>
      @if (standard().coreSummary) {
        <p class="core-sum">{{ standard().coreSummary }}</p>
      }
      <ng-content />
    </mat-expansion-panel>
  `,
  styles: `
    :host {
      display: block;
      margin-bottom: var(--space-3);
    }
    .std-panel {
      border-radius: var(--radius-card) !important;
      border: var(--border-width-hairline) solid var(--border-color) !important;
      background: var(--card-bg) !important;
      box-shadow: var(--shadow-sm) !important;
    }
    .std-code {
      font-weight: var(--font-weight-bold);
      margin-right: 0.625rem;
      color: var(--text-primary);
    }
    .std-risk {
      font-size: 0.6875rem;
      text-transform: uppercase;
      font-weight: var(--font-weight-bold);
    }
    .std-risk[data-risk='high'] {
      color: var(--danger-color);
    }
    .std-risk[data-risk='medium'] {
      color: var(--warning-color);
    }
    .std-risk[data-risk='low'] {
      color: var(--success-color);
    }
    .pct {
      font-weight: var(--font-weight-bold);
      color: var(--primary-color);
    }
    .std-desc {
      font-size: var(--font-size-caption);
      line-height: 1.45;
      color: var(--text-secondary);
      margin: 0 0 var(--space-2);
    }
    .core-sum {
      font-size: var(--font-size-caption);
      color: var(--text-secondary);
      margin: 0 0 var(--space-3);
    }
  `,
})
export class StandardSectionComponent {
  readonly standard = input.required<ComplianceStandard>();
  /** Control expanded state from parent for lazy body */
  readonly expanded = input(false);
  readonly opened = output<void>();
}
