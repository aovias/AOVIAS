import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideRefreshCw } from '@ng-icons/lucide';

@Component({
  selector: 'app-accreditation-progress-card',
  standalone: true,
  imports: [CommonModule, NgIcon],
  viewProviders: [provideIcons({ lucideRefreshCw })],
  templateUrl: './accreditation-progress-card.component.html',
  styleUrl: './accreditation-progress-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccreditationProgressCardComponent {
  @Input() title = 'NABH Accreditation Progress';
  @Input() subtitle = 'Estimated Audit Readiness';
  @Input() progress = 0;
  @Input() loading = false;

  @Output() readonly refresh = new EventEmitter<void>();

  get valueNow(): number {
    return this.clampedProgress;
  }

  get clampedProgress(): number {
    const p = Number(this.progress);
    if (!Number.isFinite(p)) return 0;
    return Math.max(0, Math.min(100, Math.round(p)));
  }

  get progressTier(): 'low' | 'mid' | 'high' {
    const p = this.clampedProgress;
    if (p < 50) return 'low';
    if (p < 80) return 'mid';
    return 'high';
  }

  onRefreshClick(): void {
    this.refresh.emit();
  }
}
