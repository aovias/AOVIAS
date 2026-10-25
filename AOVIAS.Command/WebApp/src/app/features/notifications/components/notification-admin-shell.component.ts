import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * Stage 5A: the Notifications tab's own nav was removed — see `ai-admin-shell.component.ts`.
 */
@Component({
  selector: 'app-notification-admin-shell',
  standalone: true,
  imports: [RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="notification-admin-shell">
      <router-outlet />
    </div>
  `,
  styles: [
    `
      .notification-admin-shell {
        display: flex;
        flex-direction: column;
        gap: 20px;
      }
    `,
  ],
})
export class NotificationAdminShellComponent {}
