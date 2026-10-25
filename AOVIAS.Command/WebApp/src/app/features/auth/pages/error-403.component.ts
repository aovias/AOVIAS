import { Component, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-error-403',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="error-container">
      <h1>403</h1>
      <h2>Forbidden</h2>
      <p>You don't have permission to access this resource.</p>
      <button (click)="goHome()">Go to Dashboard</button>
    </div>
  `,
  styles: [
    `
      .error-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 100vh;
        text-align: center;
      }
      h1 {
        font-size: 4rem;
        margin: 0;
      }
      h2 {
        font-size: 2rem;
        margin: 0.5rem 0;
      }
      button {
        margin-top: 2rem;
        padding: 0.75rem 1.5rem;
        background: var(--color-primary);
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
      }
    `,
  ],
})
export class Error403Component {
  constructor(private router: Router) {}

  goHome(): void {
    this.router.navigate(['/app/dashboard']);
  }
}
