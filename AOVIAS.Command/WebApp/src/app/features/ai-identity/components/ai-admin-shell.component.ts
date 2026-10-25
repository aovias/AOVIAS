import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * Stage 5A: this tab's own nav was removed. The Administration Workspace tab strip selects
 * the domain and the workspace aside lists its pages, so an in-page nav repeating the same
 * items was a duplicate.
 */
@Component({
  selector: 'app-ai-admin-shell',
  standalone: true,
  imports: [RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ai-admin-shell">
      <div class="ai-admin-shell__content">
        <router-outlet />
      </div>
    </div>
  `,
  styles: [
    `
      .ai-admin-shell {
        display: flex;
        flex-direction: column;
        gap: 20px;
        animation: aiAdminFadeIn 0.28s ease;
      }
      .ai-admin-shell__content {
        min-width: 0;
      }
      @keyframes aiAdminFadeIn {
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
export class AiAdminShellComponent {}
