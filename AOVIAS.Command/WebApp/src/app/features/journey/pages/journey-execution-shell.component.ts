import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** Parent for Journey Execution child routes; {@link JourneyWorkspaceStore} is provided by {@link JourneyFeatureShellComponent}. */
@Component({
  selector: 'app-journey-execution-shell',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JourneyExecutionShellComponent {}
