import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideAlertTriangle, lucideLightbulb } from '@ng-icons/lucide';

export type RecommendationAction = 'primary' | 'secondary';

@Component({
  selector: 'app-recommendation-panel',
  standalone: true,
  imports: [CommonModule, NgIcon],
  viewProviders: [provideIcons({ lucideLightbulb, lucideAlertTriangle })],
  templateUrl: './recommendation-panel.component.html',
  styleUrl: './recommendation-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RecommendationPanelComponent {
  @Input({ required: true }) title = '';
  @Input({ required: true }) message = '';
  @Input() highRiskCount = 0;
  @Input() lastActivity = '';
  @Input() primaryLabel = 'Open recommendations';
  @Input() secondaryLabel = 'View history';

  @Output() readonly actionClick = new EventEmitter<RecommendationAction>();
}
