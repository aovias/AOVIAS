import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCircleAlert,
  lucideCirclePercent,
  lucideListTodo,
  lucideTrendingDown,
} from '@ng-icons/lucide';
import { MetricCardIconName } from '../../models/journey-dashboard-vm.model';

@Component({
  selector: 'app-metric-card',
  standalone: true,
  imports: [CommonModule, NgIcon],
  viewProviders: [
    provideIcons({
      lucideCirclePercent,
      lucideCircleAlert,
      lucideTrendingDown,
      lucideListTodo,
    }),
  ],
  templateUrl: './metric-card.component.html',
  styleUrl: './metric-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MetricCardComponent {
  @Input({ required: true }) iconName!: MetricCardIconName;
  @Input({ required: true }) label = '';
  @Input({ required: true }) value = '';
  @Input() trendLabel: string | null = null;
  @Input() trendDirection: 'up' | 'down' | 'neutral' = 'neutral';
}
