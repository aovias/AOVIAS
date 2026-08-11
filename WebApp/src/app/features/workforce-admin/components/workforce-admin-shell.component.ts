import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * Stage 5A: this tab's own nav was removed — see `ai-admin-shell.component.ts`.
 */
@Component({
  selector: 'app-workforce-admin-shell',
  standalone: true,
  imports: [RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="wf-admin-shell">
      <div class="wf-admin-shell__content">
        <router-outlet />
      </div>
    </div>
  `,
  styles: [
    `
      .wf-admin-shell {
        display: flex;
        flex-direction: column;
        gap: 20px;
        animation: wfAdminFadeIn 0.28s ease;
      }
      .wf-admin-shell__content {
        min-width: 0;
      }
      @keyframes wfAdminFadeIn {
        from {
          opacity: 0;
          transform: translateY(4px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }
    `,
  ],
})
export class WorkforceAdminShellComponent {}
