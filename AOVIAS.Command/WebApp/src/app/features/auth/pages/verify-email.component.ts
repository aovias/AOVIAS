import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="auth-page">
      <h1>Email verification</h1>
      @if (loading()) {
        <p>Verifying…</p>
      } @else if (success()) {
        <p>Your email has been verified. You can now sign in.</p>
        <a routerLink="/login">Go to sign in</a>
      } @else {
        <p>{{ message() }}</p>
      }
    </div>
  `,
  styles: ['.auth-page { max-width: 480px; margin: 3rem auto; padding: 1rem; }'],
})
export class VerifyEmailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private auth = inject(AuthService);

  loading = signal(true);
  success = signal(false);
  message = signal('Verification failed.');

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!token) {
      this.loading.set(false);
      this.message.set('Missing verification token.');
      return;
    }

    this.auth.verifyEmail(token).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: () => {
        this.loading.set(false);
        this.message.set('Invalid or expired verification link.');
      },
    });
  }
}
