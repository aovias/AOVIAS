import { ChangeDetectionStrategy, Component } from '@angular/core';
import { JourneyFrameworkContextComponent } from '../journey-framework-context/journey-framework-context.component';

/** Primary journey workspace header: framework identity. */
@Component({
  selector: 'app-journey-workspace-primary-header',
  standalone: true,
  imports: [JourneyFrameworkContextComponent],
  templateUrl: './journey-workspace-primary-header.component.html',
  styleUrl: './journey-workspace-primary-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JourneyWorkspacePrimaryHeaderComponent {}
