import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCalendarClock,
  lucideClipboardList,
  lucideHistory,
  lucidePlay,
  lucideUpload,
} from '@ng-icons/lucide';
import { JourneyContextPanelVm } from '../../models/execution-screen-vm.model';
import { JourneyOperationalModeUiPolicy } from '../../models/journey-operational-mode-ui.model';
import { ObligationStateSectionComponent } from '../obligation-state-section/obligation-state-section.component';

@Component({
  selector: 'app-journey-context-panel',
  standalone: true,
  imports: [CommonModule, NgIcon, ObligationStateSectionComponent],
  viewProviders: [
    provideIcons({
      lucidePlay,
      lucideUpload,
      lucideClipboardList,
      lucideCalendarClock,
      lucideHistory,
    }),
  ],
  templateUrl: './journey-context-panel.component.html',
  styleUrl: './journey-context-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JourneyContextPanelComponent {
  readonly vm = input.required<JourneyContextPanelVm>();
  readonly modeUi = input<JourneyOperationalModeUiPolicy | null>(null);

  readonly startJourney = output<void>();
  readonly viewHistory = output<void>();
  readonly uploadEvidence = output<void>();
  readonly addTask = output<void>();
  readonly viewTimeline = output<void>();
  readonly viewDeficiencies = output<void>();

  onStartJourney(): void {
    this.startJourney.emit();
  }

  onViewHistory(): void {
    this.viewHistory.emit();
  }
}
