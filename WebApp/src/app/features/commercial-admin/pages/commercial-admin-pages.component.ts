import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommercialAdminApiService } from '../services/commercial-admin-api.service';

const RECENT_KEY = 'commercial-admin.recent-searches';
const FAV_KEY = 'commercial-admin.favorites';

function promptReason(action: string): string | null {
  const reason = window.prompt(`Reason for: ${action} (required)`);
  if (!reason || reason.trim().length < 3) {
    window.alert('A reason of at least 3 characters is required.');
    return null;
  }
  if (!window.confirm(`Confirm ${action}?`)) return null;
  return reason.trim();
}

function pageStyles(): string {
  return `
    .page h1 { margin: 0 0 8px; font-size: 22px; }
    .sub { color: #6b7280; margin-bottom: 20px; font-size: 14px; }
    .toolbar { display: flex; gap: 8px; margin-bottom: 16px; flex-wrap: wrap; }
    input, select, textarea { padding: 8px 10px; border: 1px solid #d1d5db; border-radius: 6px; font-size: 14px; min-width: 200px; }
    button { padding: 8px 12px; border-radius: 6px; border: 1px solid #d1d5db; background: #fff; cursor: pointer; font-size: 13px; }
    button.primary { background: #0b5fff; color: #fff; border-color: #0b5fff; }
    button.danger { background: #b91c1c; color: #fff; border-color: #b91c1c; }
    table { width: 100%; border-collapse: collapse; background: #fff; border-radius: 8px; overflow: hidden; }
    th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid #e5e7eb; font-size: 13px; }
    th { background: #f9fafb; font-weight: 600; }
    .cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); gap: 12px; margin-bottom: 20px; }
    .card { background: #fff; border: 1px solid #e5e7eb; border-radius: 8px; padding: 14px; }
    .card strong { display: block; font-size: 12px; color: #6b7280; margin-bottom: 6px; }
    .err { color: #b91c1c; margin: 8px 0; }
    .ok { color: #047857; margin: 8px 0; }
    .panel { background: #fff; border: 1px solid #e5e7eb; border-radius: 8px; padding: 16px; margin-bottom: 16px; }
    pre { white-space: pre-wrap; font-size: 12px; background: #f9fafb; padding: 12px; border-radius: 6px; max-height: 320px; overflow: auto; }
    a { color: #0b5fff; }
  `;
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  selector: 'app-ca-dashboard',
  template: `
    <div class="page">
      <h1>Operational dashboard</h1>
      <p class="sub">Provisioning, license, trial, and automation health for AOVIAS operators.</p>
      <div class="cards" *ngIf="health() as h">
        <div class="card"><strong>Active orgs</strong>{{ h.activeOrganizations ?? '—' }}</div>
        <div class="card"><strong>Suspended</strong>{{ h.suspendedOrganizations ?? '—' }}</div>
        <div class="card"><strong>Trials</strong>{{ h.trialLicenses ?? '—' }}</div>
        <div class="card"><strong>Expired licenses</strong>{{ h.expiredLicenses ?? '—' }}</div>
      </div>
      <div class="panel" *ngIf="lifecycle() as lc">
        <h3>Trial lifecycle automation</h3>
        <div class="cards">
          <div class="card"><strong>Active trials</strong>{{ lc.activeTrials ?? '—' }}</div>
          <div class="card"><strong>Expiring ≤7d</strong>{{ lc.expiringWithin7Days ?? '—' }}</div>
          <div class="card"><strong>Grace</strong>{{ lc.graceTrials ?? '—' }}</div>
          <div class="card"><strong>Expired / read-only</strong>{{ lc.expiredTrials ?? '—' }}</div>
        </div>
        <p *ngIf="lc.lastRun as r" class="sub">
          Last run: {{ r.trigger }} · processed {{ r.processedCount }} · reminders {{ r.reminderCount }} ·
          failures {{ r.failureCount }} · {{ r.finishedAtUtc | date: 'medium' }}
        </p>
        <button class="primary" (click)="runNow()" [disabled]="busy()">Run evaluation now</button>
        <p class="ok" *ngIf="runMsg()">{{ runMsg() }}</p>
      </div>
      <p class="err" *ngIf="error()">{{ error() }}</p>
      <button class="primary" (click)="load()">Refresh</button>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialDashboardPage implements OnInit {
  private readonly api = inject(CommercialAdminApiService);
  readonly health = signal<any>(null);
  readonly lifecycle = signal<any>(null);
  readonly error = signal<string | null>(null);
  readonly runMsg = signal<string | null>(null);
  readonly busy = signal(false);
  ngOnInit() {
    this.load();
  }
  load() {
    this.error.set(null);
    this.api.health().subscribe({
      next: (h) => this.health.set(h),
      error: (e) => this.error.set(e?.message || 'Failed to load health'),
    });
    this.api.lifecycleHealth().subscribe({
      next: (h) => this.lifecycle.set(h),
      error: () => this.lifecycle.set(null),
    });
  }
  runNow() {
    const reason = promptReason('run trial lifecycle evaluation');
    if (!reason) return;
    this.busy.set(true);
    this.api.runLifecycle(reason).subscribe({
      next: (r) => {
        this.runMsg.set(`Run complete: processed ${r?.processedCount ?? '—'}`);
        this.busy.set(false);
        this.load();
      },
      error: (e) => {
        this.error.set(e?.message || 'Run failed');
        this.busy.set(false);
      },
    });
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  selector: 'app-ca-orgs',
  template: `
    <div class="page">
      <h1>Organizations</h1>
      <div class="toolbar">
        <input [(ngModel)]="q" placeholder="Search name / platform id" (keyup.enter)="search()" />
        <select [(ngModel)]="status">
          <option value="">Any status</option>
          <option>Active</option>
          <option>Suspended</option>
          <option>Archived</option>
          <option>Deleted</option>
        </select>
        <button class="primary" (click)="search()">Search</button>
      </div>
      <p class="err" *ngIf="error()">{{ error() }}</p>
      <table *ngIf="items().length">
        <thead>
          <tr><th>Name</th><th>Status</th><th>Hospitals</th><th>Users</th><th></th></tr>
        </thead>
        <tbody>
          <tr *ngFor="let o of items()">
            <td>{{ o.name }}</td>
            <td>{{ o.status }}</td>
            <td>{{ o.hospitalCount }}</td>
            <td>{{ o.memberCount }}</td>
            <td>
              <a [routerLink]="['/commercial-admin/organizations', o.id]">Open</a>
              ·
              <a [routerLink]="['/commercial-admin/support', o.id]">Support</a>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialOrganizationsPage {
  private readonly api = inject(CommercialAdminApiService);
  q = '';
  status = '';
  readonly items = signal<any[]>([]);
  readonly error = signal<string | null>(null);
  search() {
    this.error.set(null);
    this.api.searchOrgs(this.q, this.status || undefined).subscribe({
      next: (r) => this.items.set(r?.items ?? r?.Items ?? []),
      error: (e) => this.error.set(e?.message || 'Search failed'),
    });
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  selector: 'app-ca-org-detail',
  template: `
    <div class="page">
      <h1>Organization</h1>
      <p class="err" *ngIf="error()">{{ error() }}</p>
      <div class="panel" *ngIf="org() as o">
        <p><strong>{{ o.name }}</strong> · {{ o.status }} · {{ o.platformId }}</p>
        <p>{{ o.type }} · {{ o.address }}</p>
        <div class="toolbar">
          <button (click)="setStatus('Suspended')">Suspend</button>
          <button (click)="setStatus('Active')">Activate / Restore</button>
          <button (click)="setStatus('Archived')">Archive</button>
          <button class="danger" (click)="setStatus('Deleted')">Soft delete</button>
          <a [routerLink]="['/commercial-admin/support', id]">Support workspace</a>
        </div>
      </div>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialOrganizationDetailPage implements OnInit {
  private readonly api = inject(CommercialAdminApiService);
  private readonly route = inject(ActivatedRoute);
  id = '';
  readonly org = signal<any>(null);
  readonly error = signal<string | null>(null);
  ngOnInit() {
    this.id = this.route.snapshot.paramMap.get('id') || '';
    this.api.getOrg(this.id).subscribe({
      next: (o) => this.org.set(o),
      error: (e) => this.error.set(e?.message || 'Load failed'),
    });
  }
  setStatus(status: string) {
    const reason = promptReason(`set status to ${status}`);
    if (!reason) return;
    this.api.setOrgStatus(this.id, status, reason).subscribe({
      next: () => this.ngOnInit(),
      error: (e) => this.error.set(e?.message || 'Update failed'),
    });
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  selector: 'app-ca-users',
  template: `
    <div class="page">
      <h1>Users</h1>
      <div class="toolbar">
        <input [(ngModel)]="q" placeholder="Email / username" (keyup.enter)="search()" />
        <button class="primary" (click)="search()">Search</button>
      </div>
      <p class="err" *ngIf="error()">{{ error() }}</p>
      <p class="ok" *ngIf="message()">{{ message() }}</p>
      <table *ngIf="items().length">
        <thead><tr><th>Email</th><th>Status</th><th>Verified</th><th>Actions</th></tr></thead>
        <tbody>
          <tr *ngFor="let u of items()">
            <td>{{ u.email }}</td>
            <td>{{ u.status }}</td>
            <td>{{ u.emailConfirmed }}</td>
            <td>
              <button (click)="act(u.userId, 'unlock')">Unlock</button>
              <button (click)="act(u.userId, 'activate')">Activate</button>
              <button (click)="act(u.userId, 'suspend')">Suspend</button>
              <button (click)="act(u.userId, 'force-password-change')">Force pwd</button>
              <button (click)="act(u.userId, 'terminate-sessions')">Kill sessions</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialUsersPage {
  private readonly api = inject(CommercialAdminApiService);
  q = '';
  readonly items = signal<any[]>([]);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  search() {
    this.api.searchUsers(this.q).subscribe({
      next: (r) => this.items.set(r?.items ?? r?.Items ?? []),
      error: (e) => this.error.set(e?.message || 'Search failed'),
    });
  }
  act(userId: string, action: string) {
    const reason = promptReason(action);
    if (!reason) return;
    this.api.userAction(userId, action, reason).subscribe({
      next: () => this.message.set(`${action} ok`),
      error: (e) => this.error.set(e?.message || 'Action failed'),
    });
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  selector: 'app-ca-trials',
  template: `
    <div class="page">
      <h1>Trials</h1>
      <div class="toolbar">
        <input [(ngModel)]="q" placeholder="Search trials" (keyup.enter)="search()" />
        <button class="primary" (click)="search()">Search</button>
      </div>
      <div class="toolbar">
        <input [(ngModel)]="orgId" placeholder="Organization Id for actions" />
        <button (click)="extend()">Extend</button>
        <button (click)="expire()">Expire</button>
        <button (click)="convert()">Convert to paid</button>
      </div>
      <p class="err" *ngIf="error()">{{ error() }}</p>
      <p class="ok" *ngIf="message()">{{ message() }}</p>
      <table *ngIf="items().length">
        <thead><tr><th>Org</th><th>Status</th><th>Expires</th><th>Seats</th><th>Ext</th></tr></thead>
        <tbody>
          <tr *ngFor="let t of items()">
            <td>{{ t.organizationId }}</td>
            <td>{{ t.status }}</td>
            <td>{{ t.expiresAtUtc | date: 'medium' }}</td>
            <td>{{ t.usedSeats }}/{{ t.maxSeats }}</td>
            <td>{{ t.extensionCount }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialTrialsPage {
  private readonly api = inject(CommercialAdminApiService);
  q = '';
  orgId = '';
  readonly items = signal<any[]>([]);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  search() {
    this.api.searchTrials(this.q).subscribe({
      next: (r) => this.items.set(r?.items ?? r?.Items ?? []),
      error: (e) => this.error.set(e?.message || 'Search failed'),
    });
  }
  extend() {
    const reason = promptReason('extend trial');
    if (!reason || !this.orgId) return;
    this.api.extendTrial(this.orgId, 15, reason).subscribe({
      next: () => this.message.set('Extended'),
      error: (e) => this.error.set(e?.message || 'Failed'),
    });
  }
  expire() {
    const reason = promptReason('expire trial');
    if (!reason || !this.orgId) return;
    this.api.expireTrial(this.orgId, reason).subscribe({
      next: () => this.message.set('Expired'),
      error: (e) => this.error.set(e?.message || 'Failed'),
    });
  }
  convert() {
    const reason = promptReason('convert trial');
    if (!reason || !this.orgId) return;
    this.api.convertTrial(this.orgId, reason).subscribe({
      next: () => this.message.set('Converted'),
      error: (e) => this.error.set(e?.message || 'Failed'),
    });
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  selector: 'app-ca-licenses',
  template: `
    <div class="page">
      <h1>Licenses</h1>
      <p class="sub">License inventory is the operational source of truth. Subscription plans are catalogue metadata only.</p>
      <div class="toolbar">
        <input [(ngModel)]="q" placeholder="Search licenses" (keyup.enter)="search()" />
        <button class="primary" (click)="search()">Search</button>
        <button (click)="backfill()">Run catalogue backfill</button>
        <input [(ngModel)]="summaryOrgId" placeholder="Org id for summary" />
        <button (click)="loadSummary()">Licensing summary</button>
      </div>
      <pre *ngIf="summary()">{{ summary() | json }}</pre>
      <p class="ok" *ngIf="backfillMsg()">{{ backfillMsg() }}</p>
      <p class="err" *ngIf="error()">{{ error() }}</p>
      <table *ngIf="items().length">
        <thead><tr><th>Key</th><th>Type</th><th>Status</th><th>Org</th><th>Seats</th><th></th></tr></thead>
        <tbody>
          <tr *ngFor="let l of items()">
            <td>{{ l.keyCode }}</td>
            <td>{{ l.licenseType }}</td>
            <td>{{ l.status }}</td>
            <td>{{ l.organizationId }}</td>
            <td>{{ l.usedSeats }}/{{ l.maxSeats }}</td>
            <td><button class="danger" (click)="revoke(l.licenseKeyId)">Revoke</button></td>
          </tr>
        </tbody>
      </table>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialLicensesPage {
  private readonly api = inject(CommercialAdminApiService);
  q = '';
  summaryOrgId = '';
  readonly items = signal<any[]>([]);
  readonly summary = signal<any | null>(null);
  readonly backfillMsg = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  search() {
    this.api.searchLicenses(this.q).subscribe({
      next: (r) => this.items.set(r?.items ?? r?.Items ?? []),
      error: (e) => this.error.set(e?.message || 'Search failed'),
    });
  }
  loadSummary() {
    if (!this.summaryOrgId) return;
    this.api.licensingSummary(this.summaryOrgId).subscribe({
      next: (s) => this.summary.set(s),
      error: (e) => this.error.set(e?.message || 'Summary failed'),
    });
  }
  backfill() {
    this.api.catalogBackfill().subscribe({
      next: (r) => {
        this.backfillMsg.set(
          `Linked ${r?.licensesLinked ?? r?.LicensesLinked ?? 0}, created ${r?.catalogRowsCreated ?? r?.CatalogRowsCreated ?? 0}, drift ${r?.driftDetected ?? r?.DriftDetected ?? 0}`,
        );
        this.search();
      },
      error: (e) => this.error.set(e?.message || 'Backfill failed'),
    });
  }
  revoke(id: string) {
    const reason = promptReason('revoke license');
    if (!reason) return;
    this.api.revokeLicense(id, reason).subscribe({
      next: () => this.search(),
      error: (e) => this.error.set(e?.message || 'Revoke failed'),
    });
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  selector: 'app-ca-email',
  template: `
    <div class="page">
      <h1>Email operations</h1>
      <div class="panel">
        <div class="toolbar">
          <input [(ngModel)]="to" placeholder="To address" />
          <input [(ngModel)]="orgName" placeholder="Org name" />
          <select [(ngModel)]="template">
            <option>WelcomeWithTrial</option>
            <option>GettingStarted</option>
            <option>SecurityNotification</option>
          </select>
          <button class="primary" (click)="send()">Send</button>
          <button (click)="preview()">Preview</button>
        </div>
        <p class="ok" *ngIf="message()">{{ message() }}</p>
        <p class="err" *ngIf="error()">{{ error() }}</p>
        <pre *ngIf="previewHtml()">{{ previewHtml() }}</pre>
      </div>
      <button (click)="loadLogs()">Refresh delivery logs</button>
      <pre *ngIf="logs()">{{ logs() | json }}</pre>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialEmailPage {
  private readonly api = inject(CommercialAdminApiService);
  to = '';
  orgName = '';
  template = 'WelcomeWithTrial';
  readonly message = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly previewHtml = signal<string | null>(null);
  readonly logs = signal<any>(null);
  send() {
    const reason = promptReason('send email');
    if (!reason) return;
    this.api
      .sendEmail({
        toAddress: this.to,
        organizationName: this.orgName,
        template: this.template,
        reason,
        confirmed: true,
      })
      .subscribe({
        next: (r) => this.message.set(JSON.stringify(r)),
        error: (e) => this.error.set(e?.message || 'Send failed'),
      });
  }
  preview() {
    this.api.previewEmail(this.template).subscribe({
      next: (r) => this.previewHtml.set(r?.html || r?.Html || ''),
      error: (e) => this.error.set(e?.message || 'Preview failed'),
    });
  }
  loadLogs() {
    this.api.emailLogs().subscribe({
      next: (r) => this.logs.set(r),
      error: (e) => this.error.set(e?.message || 'Logs failed'),
    });
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  selector: 'app-ca-support',
  template: `
    <div class="page">
      <h1>Customer support workspace</h1>
      <div class="toolbar">
        <input [(ngModel)]="orgId" placeholder="Organization Id" />
        <button class="primary" (click)="load()">Load 360</button>
      </div>
      <p class="err" *ngIf="error()">{{ error() }}</p>
      <div class="panel" *ngIf="data() as d">
        <h3>Quick actions</h3>
        <div class="toolbar">
          <button (click)="quick('extend')">Extend trial</button>
          <button (click)="quick('suspend-org')">Suspend org</button>
          <button routerLink="/commercial-admin/email">Resend email</button>
        </div>
        <pre>{{ d | json }}</pre>
      </div>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialSupportPage implements OnInit {
  private readonly api = inject(CommercialAdminApiService);
  private readonly route = inject(ActivatedRoute);
  orgId = '';
  readonly data = signal<any>(null);
  readonly error = signal<string | null>(null);
  ngOnInit() {
    this.orgId = this.route.snapshot.paramMap.get('orgId') || '';
    if (this.orgId) this.load();
  }
  load() {
    this.api.support(this.orgId).subscribe({
      next: (d) => this.data.set(d),
      error: (e) => this.error.set(e?.message || 'Load failed'),
    });
  }
  quick(kind: string) {
    if (kind === 'extend') {
      const reason = promptReason('extend trial');
      if (!reason) return;
      this.api.extendTrial(this.orgId, 15, reason).subscribe({ next: () => this.load() });
    }
    if (kind === 'suspend-org') {
      const reason = promptReason('suspend org');
      if (!reason) return;
      this.api.setOrgStatus(this.orgId, 'Suspended', reason).subscribe({ next: () => this.load() });
    }
  }
}

@Component({
  standalone: true,
  imports: [CommonModule],
  selector: 'app-ca-ops',
  template: `
    <div class="page">
      <h1>Commercial operations</h1>
      <p class="sub">Trial → paid conversion and seat expansion use Trials / Licenses modules. Payment gateways are out of scope.</p>
      <ul>
        <li>Convert trial: Trials page → Convert to paid (issues Subscription LicenseKey)</li>
        <li>Seat expansion: Licenses seats API (reason required)</li>
        <li>Catalogue metadata: /api/v1/subscriptions (plan linkage only — not operational authority)</li>
        <li>Unified read model: /api/v1/licensing/summary</li>
      </ul>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialOpsPage {}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  selector: 'app-ca-audit',
  template: `
    <div class="page">
      <h1>Audit timeline</h1>
      <div class="toolbar">
        <input [(ngModel)]="orgId" placeholder="Filter by org id" />
        <button class="primary" (click)="load()">Load</button>
      </div>
      <pre *ngIf="rows()">{{ rows() | json }}</pre>
      <p class="err" *ngIf="error()">{{ error() }}</p>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialAuditPage {
  private readonly api = inject(CommercialAdminApiService);
  orgId = '';
  readonly rows = signal<any>(null);
  readonly error = signal<string | null>(null);
  load() {
    this.api.audit(this.orgId || undefined).subscribe({
      next: (r) => this.rows.set(r),
      error: (e) => this.error.set(e?.message || 'Failed'),
    });
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  selector: 'app-ca-search',
  template: `
    <div class="page">
      <h1>Global search</h1>
      <p class="sub">Press / anywhere in this console to focus search (when on this page).</p>
      <div class="toolbar">
        <input #box [(ngModel)]="q" placeholder="Organizations, users, licenses, trials" (keyup.enter)="run()" />
        <button class="primary" (click)="run()">Search</button>
      </div>
      <p *ngIf="recent().length">Recent: <a *ngFor="let r of recent()" href="#" (click)="useRecent($event, r)">{{ r }} </a></p>
      <p *ngIf="favorites().length">Favorites:
        <a *ngFor="let f of favorites()" [routerLink]="['/commercial-admin/support', f]">{{ f }} </a>
      </p>
      <pre *ngIf="result()">{{ result() | json }}</pre>
      <p class="err" *ngIf="error()">{{ error() }}</p>
    </div>
  `,
  styles: [pageStyles()],
})
export class CommercialSearchPage implements OnInit, OnDestroy {
  private readonly api = inject(CommercialAdminApiService);
  q = '';
  readonly result = signal<any>(null);
  readonly error = signal<string | null>(null);
  readonly recent = signal<string[]>([]);
  readonly favorites = signal<string[]>([]);
  private keyHandler = (e: KeyboardEvent) => {
    if (e.key === '/' && (e.target as HTMLElement)?.tagName !== 'INPUT') {
      e.preventDefault();
      document.querySelector<HTMLInputElement>('app-ca-search input')?.focus();
    }
  };
  ngOnInit() {
    this.recent.set(JSON.parse(localStorage.getItem(RECENT_KEY) || '[]'));
    this.favorites.set(JSON.parse(localStorage.getItem(FAV_KEY) || '[]'));
    window.addEventListener('keydown', this.keyHandler);
  }
  ngOnDestroy() {
    window.removeEventListener('keydown', this.keyHandler);
  }
  run() {
    if (this.q.trim().length < 2) return;
    const list = [this.q, ...this.recent().filter((x) => x !== this.q)].slice(0, 8);
    localStorage.setItem(RECENT_KEY, JSON.stringify(list));
    this.recent.set(list);
    this.api.globalSearch(this.q).subscribe({
      next: (r) => this.result.set(r),
      error: (e) => this.error.set(e?.message || 'Search failed'),
    });
  }
  useRecent(ev: Event, r: string) {
    ev.preventDefault();
    this.q = r;
    this.run();
  }
}
