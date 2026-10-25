import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-maintenance',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="maintenance-container">
      <h1>Maintenance Mode</h1>
      <p>We're currently performing scheduled maintenance.</p>
      <p>Please check back soon.</p>
    </div>
  `,
  styles: [
    `
      .maintenance-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100vh;
        text-align: center;
      }
      h1 {
        font-size: 3rem;
        margin: 0;
      }
      p {
        font-size: 1.2rem;
        margin: 1rem 0;
      }
    `,
  ],
})
export class MaintenanceComponent {}
