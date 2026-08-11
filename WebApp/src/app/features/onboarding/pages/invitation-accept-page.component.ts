import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-invitation-accept-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="wrap">
      <h1>Accept invitation</h1>
      @if (preview(); as p) {
        <p class="sub">
          Join <strong>{{ p.organizationName || 'organization' }}</strong> as
          <strong>{{ p.role }}</strong>
          @if (p.designation) {
            <span> ({{ p.designation }})</span>
          }
        </p>
        <p class="meta">Invited email: {{ p.email }} · Expires {{ p.expiresAt | date: 'medium' }}</p>
        <p class="meta" *ngIf="p.requireSeat">A license seat will be assigned on acceptance.</p>

        @if (p.status !== 'Pending') {
          <p class="err">This invitation is {{ p.status }} and cannot be accepted.</p>
        } @else {
          <div class="form">
            <label>First name <input [(ngModel)]="firstName" /></label>
            <label>Last name <input [(ngModel)]="lastName" /></label>
            <label>Create password <input type="password" [(ngModel)]="password" /></label>
            <p class="hint">If you already have an account, sign in first then return to this link.</p>
            <button type="button" class="primary" [disabled]="busy()" (click)="accept()">
              {{ busy() ? 'Working…' : 'Accept &amp; join' }}
            </button>
          </div>
        }
      } @else if (!error()) {
        <p>Loading invitation…</p>
      }

      @if (success(); as s) {
        <div class="ok">
          <p>{{ s.message || 'You are now an operational team member.' }}</p>
          <p *ngIf="s.seatAssigned">Seat assigned.</p>
          <a routerLink="/login">Continue to sign in</a>
        </div>
      }
      @if (error()) {
        <p class="err">{{ error() }}</p>
      }
    </div>
  `,
  styles: [
    `
      .wrap {
        max-width: 520px;
        margin: 3rem auto;
        padding: 1.5rem;
        font-family: Segoe UI, system-ui, sans-serif;
      }
      .sub {
        font-size: 1.05rem;
      }
      .meta,
      .hint {
        color: #6b7280;
        font-size: 0.9rem;
      }
      .form {
        display: grid;
        gap: 0.75rem;
        margin-top: 1rem;
      }
      label {
        display: grid;
        gap: 0.25rem;
        font-size: 0.9rem;
      }
      input {
        padding: 0.5rem 0.65rem;
        border: 1px solid #d1d5db;
        border-radius: 6px;
      }
      .primary {
        background: #0b5fff;
        color: #fff;
        border: 0;
        padding: 0.65rem 1rem;
        border-radius: 6px;
        cursor: pointer;
      }
      .err {
        color: #b91c1c;
      }
      .ok {
        margin-top: 1rem;
        padding: 1rem;
        background: #ecfdf5;
        border-radius: 8px;
      }
    `,
  ],
})
export class InvitationAcceptPageComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  token = '';
  firstName = '';
  lastName = '';
  password = '';
  readonly preview = signal<any | null>(null);
  readonly success = signal<any | null>(null);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') || '';
    if (!this.token) {
      this.error.set('Missing invitation token.');
      return;
    }
    this.http
      .get<any>(`${environment.apiBaseUrl}/invitations/redeem/preview`, {
        params: { token: this.token },
      })
      .subscribe({
        next: (res) => this.preview.set(res?.data ?? res),
        error: () => this.error.set('Invitation not found or expired.'),
      });
  }

  accept(): void {
    this.busy.set(true);
    this.error.set(null);
    this.http
      .post<any>(`${environment.apiBaseUrl}/invitations/redeem`, {
        token: this.token,
        password: this.password,
        firstName: this.firstName,
        lastName: this.lastName,
      })
      .subscribe({
        next: (res) => {
          this.success.set(res?.data ?? res);
          this.busy.set(false);
        },
        error: (e) => {
          this.error.set(e?.error?.error?.message || e?.error?.message || 'Accept failed');
          this.busy.set(false);
        },
      });
  }
}
