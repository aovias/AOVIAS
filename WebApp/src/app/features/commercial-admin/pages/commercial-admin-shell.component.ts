import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-commercial-admin-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="ca-shell">
      <aside class="ca-nav">
        <div class="brand">AOVIAS Commercial Admin</div>
        <nav>
          <a *ngFor="let item of nav" [routerLink]="item.path" routerLinkActive="active">{{ item.label }}</a>
        </nav>
        <a class="back" routerLink="/app/dashboard">← Customer app</a>
      </aside>
      <main class="ca-main">
        <router-outlet />
      </main>
    </div>
  `,
  styles: [
    `
      .ca-shell {
        display: grid;
        grid-template-columns: 240px 1fr;
        min-height: 100vh;
        background: #f3f4f6;
        color: #111827;
        font-family: 'Segoe UI', system-ui, sans-serif;
      }
      .ca-nav {
        background: #0f172a;
        color: #e2e8f0;
        padding: 20px 16px;
        display: flex;
        flex-direction: column;
        gap: 16px;
      }
      .brand {
        font-weight: 700;
        font-size: 14px;
        letter-spacing: 0.02em;
        padding-bottom: 12px;
        border-bottom: 1px solid #334155;
      }
      nav {
        display: flex;
        flex-direction: column;
        gap: 4px;
        flex: 1;
      }
      a {
        color: #cbd5e1;
        text-decoration: none;
        padding: 8px 10px;
        border-radius: 6px;
        font-size: 13px;
      }
      a.active,
      a:hover {
        background: #1e293b;
        color: #fff;
      }
      .back {
        font-size: 12px;
        opacity: 0.8;
      }
      .ca-main {
        padding: 24px 28px;
        overflow: auto;
      }
      @media (max-width: 900px) {
        .ca-shell {
          grid-template-columns: 1fr;
        }
        .ca-nav {
          flex-direction: row;
          flex-wrap: wrap;
          align-items: center;
        }
        nav {
          flex-direction: row;
          flex-wrap: wrap;
        }
      }
    `,
  ],
})
export class CommercialAdminShellComponent {
  readonly nav = [
    { path: '/commercial-admin/dashboard', label: 'Dashboard' },
    { path: '/commercial-admin/organizations', label: 'Organizations' },
    { path: '/commercial-admin/users', label: 'Users' },
    { path: '/commercial-admin/trials', label: 'Trials' },
    { path: '/commercial-admin/licenses', label: 'Licenses' },
    { path: '/commercial-admin/email', label: 'Email' },
    { path: '/commercial-admin/support', label: 'Support' },
    { path: '/commercial-admin/commercial', label: 'Commercial Ops' },
    { path: '/commercial-admin/audit', label: 'Audit' },
    { path: '/commercial-admin/search', label: 'Search' },
  ];
}
