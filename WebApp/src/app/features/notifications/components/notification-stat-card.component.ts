import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { AdminStatCardComponent } from '../../../shared/ui/administration/admin-stat-card.component';

/** @deprecated Prefer AdminStatCardComponent — retained for selector compatibility. */
@Component({
  selector: 'app-notification-stat-card',
  standalone: true,
  imports: [AdminStatCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-admin-stat-card [label]="label" [value]="value" [placeholder]="placeholder" />
  `,
})
export class NotificationStatCardComponent {
  @Input({ required: true }) label!: string;
  @Input({ required: true }) value!: string | number;
  @Input() placeholder = false;
}
