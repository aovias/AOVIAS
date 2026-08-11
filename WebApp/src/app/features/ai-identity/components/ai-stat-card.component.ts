import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { AdminStatCardComponent } from '../../../shared/ui/administration/admin-stat-card.component';

/** @deprecated Prefer AdminStatCardComponent — retained for selector compatibility. */
@Component({
  selector: 'app-ai-stat-card',
  standalone: true,
  imports: [AdminStatCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-admin-stat-card [label]="label" [value]="value" [hint]="hint" [placeholder]="placeholder" />
  `,
})
export class AiStatCardComponent {
  @Input({ required: true }) label!: string;
  @Input({ required: true }) value!: string | number;
  @Input() hint = '';
  @Input() placeholder = false;
}
