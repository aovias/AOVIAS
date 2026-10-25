import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import {
  AdminPageStateComponent,
  AdminPageStateKind,
} from '../../../shared/ui/administration/admin-page-state.component';

@Component({
  selector: 'app-workforce-admin-page-state',
  standalone: true,
  imports: [AdminPageStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-admin-page-state
      [state]="state"
      [message]="message"
      [icon]="icon"
      [showRetry]="showRetry"
      (retry)="retry.emit()"
    />
  `,
})
export class WorkforceAdminPageStateComponent {
  @Input() state: AdminPageStateKind = 'empty';
  @Input() message = '';
  @Input() icon = '';
  @Input() showRetry = true;
  @Output() retry = new EventEmitter<void>();
}
