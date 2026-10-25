import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { UserSessionDto } from '../../../core/auth/models/login-response.model';
import { SecurityApiService } from '../services/security-api.service';
import { NotificationService } from '../../../core/ui/services/notification.service';

@Component({
  selector: 'app-security-sessions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page">
      <h1><a routerLink="..">Security</a> / Sessions</h1>
      @for (s of sessions(); track s.sessionId) {
        <div class="row">
          <span>{{ s.userAgent || 'Unknown' }} — {{ s.ipAddress || '—' }} {{ s.isCurrent ? '(current)' : '' }}</span>
          @if (!s.isCurrent) {
            <button type="button" (click)="revoke(s.sessionId)">Revoke</button>
          }
        </div>
      }
    </section>
  `,
  styles: [`.page { padding: 1.5rem; } .row { display: flex; justify-content: space-between; padding: 0.5rem 0; border-bottom: 1px solid #eee; }`],
})
export class SecuritySessionsComponent implements OnInit {
  private auth = inject(AuthService);
  private api = inject(SecurityApiService);
  private notify = inject(NotificationService);
  sessions = signal<UserSessionDto[]>([]);

  ngOnInit(): void {
    this.auth.listSessions().subscribe({ next: (items) => this.sessions.set(items), error: () => this.notify.showError('Unable to load sessions') });
  }

  revoke(sessionId: string): void {
    this.api.revokeSession(sessionId).subscribe({
      next: () => { this.notify.showSuccess('Session revoked'); this.ngOnInit(); },
      error: () => this.notify.showError('Revoke failed'),
    });
  }
}
