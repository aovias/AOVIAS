import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * Stage 5A: this tab's own nav was removed — see `ai-admin-shell.component.ts`.
 */
@Component({
  selector: 'app-integration-admin-shell',
  standalone: true,
  imports: [RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="integration-admin-shell">
      <router-outlet />
    </div>
  `,
  styles: [
    `
      .integration-admin-shell {
        display: flex;
        flex-direction: column;
        gap: 20px;
      }
    `,
  ],
})
export class IntegrationAdminShellComponent {}
