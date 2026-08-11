import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-person-merge-placeholder',
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="merge-placeholder">
      <a routerLink="/app/persons">← Directory</a>
      <h1>Merge duplicate persons</h1>
      <p>
        Full duplicate detection and merge workflow is planned for a future release (M6.2+).
        This page reserves the route and navigation entry for the Person Directory merge feature.
      </p>
    </section>
  `,
  styles: [
    `
      .merge-placeholder { padding: 1.5rem; max-width: 40rem; }
      p { color: #4b5563; line-height: 1.6; }
    `,
  ],
})
export class PersonMergePlaceholderComponent {}
