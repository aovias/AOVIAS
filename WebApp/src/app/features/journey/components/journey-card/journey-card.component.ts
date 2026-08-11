import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideArrowRight,
  lucideClipboardCheck,
  lucideEye,
  lucideLock,
  lucideScanSearch,
  lucideShieldCheck,
} from '@ng-icons/lucide';
import { JourneyModeCardVariant, JourneyModeIconKey } from '../../models/journey-dashboard-vm.model';

@Component({
  selector: 'app-journey-card',
  standalone: true,
  imports: [CommonModule, NgIcon],
  viewProviders: [
    provideIcons({
      lucideArrowRight,
      lucideClipboardCheck,
      lucideEye,
      lucideLock,
      lucideScanSearch,
      lucideShieldCheck,
    }),
  ],
  templateUrl: './journey-card.component.html',
  styleUrl: './journey-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JourneyCardComponent {
  @Input({ required: true }) title = '';
  @Input({ required: true }) iconKey!: JourneyModeIconKey;
  @Input() description = '';
  @Input({ required: true }) variant!: JourneyModeCardVariant;
  @Input({ required: true }) stepIndex = 1;
  @Input({ required: true }) totalSteps = 4;
  @Input() lockReason: string | null = null;
  @Input() isNextPhase = false;
  @Input() progress: number | null = null;
  @Input() progressClass = 'progress-0';

  @Output() readonly selectCard = new EventEmitter<void>();

  readonly iconNames: Record<JourneyModeIconKey, string> = {
    self: 'lucideClipboardCheck',
    internal: 'lucideScanSearch',
    final: 'lucideShieldCheck',
    surveillance: 'lucideEye',
  };

  get lucideName(): string {
    return this.iconNames[this.iconKey] ?? 'lucideClipboardCheck';
  }

  get statusLabel(): string {
    switch (this.variant) {
      case 'active':
        return 'Active';
      case 'locked':
        return 'Locked';
      default:
        return 'Available';
    }
  }

  get ariaLabel(): string {
    if (this.variant === 'locked') {
      return `${this.title}, locked. ${this.lockReason ?? 'Unavailable'}`;
    }
    if (this.variant === 'active') {
      return `${this.title}, current operating mode. Phase ${this.stepIndex} of ${this.totalSteps}.`;
    }
    return `Switch to ${this.title} mode. Phase ${this.stepIndex} of ${this.totalSteps}.`;
  }

  onActivate(): void {
    if (this.variant !== 'available') {
      return;
    }
    this.selectCard.emit();
  }

  onKeydown(event: KeyboardEvent): void {
    if (this.variant !== 'available') {
      return;
    }
    if (event.key !== 'Enter' && event.key !== ' ') {
      return;
    }
    event.preventDefault();
    this.onActivate();
  }
}
