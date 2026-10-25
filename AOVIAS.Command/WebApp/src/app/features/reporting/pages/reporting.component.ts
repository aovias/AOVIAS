import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-reporting',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="reporting-container">
    <h1>Reports</h1>
    <p>Reports page - to be implemented</p>
  </div>`,
  styles: [
    `
      .reporting-container {
        padding: var(--page-padding-block-start) var(--page-padding-inline) var(--page-padding-block-end);
      }
    `,
  ],
})
export class ReportingComponent {}
