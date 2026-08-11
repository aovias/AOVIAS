import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideAlertTriangle, lucideHistory, lucidePlay } from '@ng-icons/lucide';

export type ActionBarAction = 'primary' | 'secondary' | 'danger';

@Component({
  selector: 'app-action-bar',
  standalone: true,
  imports: [CommonModule, NgIcon],
  viewProviders: [provideIcons({ lucidePlay, lucideHistory, lucideAlertTriangle })],
  templateUrl: './action-bar.component.html',
  styleUrl: './action-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActionBarComponent {
  @Input() primaryLabel = 'Save changes';
  @Input() secondaryLabel = 'Export report';
  @Input() dangerLabel = 'Reset draft';
  /** When true, danger action is not rendered (grid becomes two columns). */
  @Input() dangerHidden = false;
  /** Stronger emphasis for danger (e.g. Final Audit). */
  @Input() dangerEmphasis = false;
  @Input() dangerDisabled = false;

  @Output() readonly actionClick = new EventEmitter<ActionBarAction>();
}
