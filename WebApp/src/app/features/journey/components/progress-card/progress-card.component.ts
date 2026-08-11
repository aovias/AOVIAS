import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-progress-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './progress-card.component.html',
  styleUrl: './progress-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProgressCardComponent {
  @Input({ required: true }) scoreLabel!: string;
  @Input({ required: true }) completionPercent!: number;
  @Input({ required: true }) complianceLabel!: string;
  @Input({ required: true }) complianceTone!:
    | 'compliant'
    | 'partial'
    | 'non-compliant'
    | 'not-applicable';
  @Input({ required: true }) evidenceSummary!: string;
  @Input({ required: true }) lastUpdatedLabel!: string;
}
