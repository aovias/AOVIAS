import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { AdminStatCardComponent } from '../../../shared/ui/administration/admin-stat-card.component';

@Component({
  selector: 'app-workforce-stat-card',
  standalone: true,
  imports: [AdminStatCardComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-admin-stat-card [label]="label" [value]="value" [hint]="hint" [placeholder]="placeholder" />
  `,
})
export class WorkforceStatCardComponent {
  @Input({ required: true }) label!: string;
  @Input({ required: true }) value!: string | number;
  @Input() hint = '';
  @Input() placeholder = false;
}
