import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatStepperModule } from '@angular/material/stepper';
import {
  HealthPingResult,
  SetupApiService,
  SetupConfigurePayload,
} from '../services/setup-api.service';

@Component({
  selector: 'app-setup-wizard-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    MatStepperModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="setup">
      <header class="setup__hero">
        <p class="setup__brand">AOVIAS</p>
        <h1>Platform installation</h1>
        <p class="setup__sub">Configure database, mail, storage, AI, and auth for this deployment.</p>
      </header>

      @if (checking()) {
        <div class="setup__loading">
          <mat-spinner diameter="36" />
          <span>Checking installation status…</span>
        </div>
      } @else {
        <mat-stepper linear class="setup__stepper">
          <mat-step [completed]="dbOk()">
            <ng-template matStepLabel>Database</ng-template>
            <mat-card class="setup__card">
              <mat-form-field appearance="outline" class="full">
                <mat-label>SQL connection string</mat-label>
                <textarea matInput rows="3" [(ngModel)]="connectionString"></textarea>
              </mat-form-field>
              <div class="setup__actions">
                <button mat-stroked-button type="button" (click)="testDb()" [disabled]="busy()">
                  Test connection
                </button>
                <button mat-flat-button color="primary" type="button" matStepperNext [disabled]="!dbOk()">
                  Next
                </button>
              </div>
              @if (dbMessage()) {
                <p [class.ok]="dbOk()" [class.err]="!dbOk()">{{ dbMessage() }}</p>
              }
            </mat-card>
          </mat-step>

          <mat-step>
            <ng-template matStepLabel>SMTP</ng-template>
            <mat-card class="setup__card">
              <mat-form-field appearance="outline" class="full">
                <mat-label>Host</mat-label>
                <input matInput [(ngModel)]="smtpHost" />
              </mat-form-field>
              <mat-form-field appearance="outline" class="full">
                <mat-label>Port</mat-label>
                <input matInput type="number" [(ngModel)]="smtpPort" />
              </mat-form-field>
              <mat-form-field appearance="outline" class="full">
                <mat-label>From address</mat-label>
                <input matInput [(ngModel)]="smtpFrom" />
              </mat-form-field>
              <mat-checkbox [(ngModel)]="smtpSsl">Use SSL</mat-checkbox>
              <div class="setup__actions">
                <button mat-button type="button" matStepperPrevious>Back</button>
                <button mat-flat-button color="primary" type="button" matStepperNext>Next</button>
              </div>
            </mat-card>
          </mat-step>

          <mat-step>
            <ng-template matStepLabel>Storage &amp; AI</ng-template>
            <mat-card class="setup__card">
              <mat-form-field appearance="outline" class="full">
                <mat-label>Storage provider</mat-label>
                <input matInput [(ngModel)]="storageProvider" />
              </mat-form-field>
              <mat-form-field appearance="outline" class="full">
                <mat-label>Storage root path</mat-label>
                <input matInput [(ngModel)]="storageRoot" />
              </mat-form-field>
              <mat-checkbox [(ngModel)]="aiEnabled">Enable AI</mat-checkbox>
              <mat-form-field appearance="outline" class="full">
                <mat-label>AI endpoint</mat-label>
                <input matInput [(ngModel)]="aiEndpoint" />
              </mat-form-field>
              <div class="setup__actions">
                <button mat-button type="button" matStepperPrevious>Back</button>
                <button mat-flat-button color="primary" type="button" matStepperNext>Next</button>
              </div>
            </mat-card>
          </mat-step>

          <mat-step>
            <ng-template matStepLabel>Auth</ng-template>
            <mat-card class="setup__card">
              <mat-form-field appearance="outline" class="full">
                <mat-label>JWT issuer</mat-label>
                <input matInput [(ngModel)]="jwtIssuer" />
              </mat-form-field>
              <mat-form-field appearance="outline" class="full">
                <mat-label>JWT audience</mat-label>
                <input matInput [(ngModel)]="jwtAudience" />
              </mat-form-field>
              <mat-form-field appearance="outline" class="full">
                <mat-label>JWT secret (min 32 chars)</mat-label>
                <input matInput type="password" [(ngModel)]="jwtSecret" />
              </mat-form-field>
              <div class="setup__actions">
                <button mat-button type="button" matStepperPrevious>Back</button>
                <button mat-flat-button color="primary" type="button" (click)="saveConfig()" [disabled]="busy()">
                  Save configuration
                </button>
                <button
                  mat-flat-button
                  color="accent"
                  type="button"
                  (click)="finish()"
                  [disabled]="busy() || !configSaved()"
                >
                  Complete installation
                </button>
              </div>
              @if (configMessage()) {
                <p class="ok">{{ configMessage() }}</p>
              }
              @if (error()) {
                <p class="err">{{ error() }}</p>
              }
              @if (pings().length) {
                <ul class="setup__pings">
                  @for (p of pings(); track p.service) {
                    <li [class.ok]="p.success" [class.err]="!p.success">
                      {{ p.service }} — {{ p.message }}
                    </li>
                  }
                </ul>
              }
            </mat-card>
          </mat-step>
        </mat-stepper>
      }
    </div>
  `,
  styles: [
    `
      .setup {
        min-height: 100vh;
        padding: 2rem 1.25rem 3rem;
        background:
          radial-gradient(ellipse at top left, var(--color-primary-100) 0%, transparent 45%),
          linear-gradient(180deg, #f8fafc 0%, var(--color-primary-50, #eef3ff) 100%);
        box-sizing: border-box;
      }
      .setup__hero {
        max-width: 720px;
        margin: 0 auto 1.5rem;
      }
      .setup__brand {
        margin: 0;
        font-size: 2rem;
        font-weight: 800;
        letter-spacing: 0.04em;
        color: #0f172a;
      }
      h1 {
        margin: 0.35rem 0;
        font-size: var(--font-size-page-title);
        color: #1e293b;
      }
      .setup__sub {
        margin: 0;
        color: #64748b;
      }
      .setup__stepper,
      .setup__loading {
        max-width: 720px;
        margin: 0 auto;
      }
      .setup__loading {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        color: #475569;
      }
      .setup__card {
        padding: 1.25rem;
        display: grid;
        gap: 0.75rem;
      }
      .full {
        width: 100%;
      }
      .setup__actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
      }
      .ok {
        color: #166534;
      }
      .err {
        color: #b91c1c;
      }
      .setup__pings {
        margin: 0;
        padding-left: 1.1rem;
      }
    `,
  ],
})
export class SetupWizardPageComponent implements OnInit {
  private readonly api = inject(SetupApiService);
  private readonly router = inject(Router);

  readonly checking = signal(true);
  readonly busy = signal(false);
  readonly dbOk = signal(false);
  readonly dbMessage = signal('');
  readonly configSaved = signal(false);
  readonly configMessage = signal('');
  readonly error = signal('');
  readonly pings = signal<HealthPingResult[]>([]);

  connectionString = '';
  smtpHost = '';
  smtpPort = 587;
  smtpFrom = '';
  smtpSsl = true;
  storageProvider = 'Local';
  storageRoot = 'App_Data/storage';
  aiEnabled = true;
  aiEndpoint = '';
  jwtIssuer = '';
  jwtAudience = '';
  jwtSecret = '';

  ngOnInit(): void {
    this.api.getStatus().subscribe({
      next: (status) => {
        if (status.completed) {
          void this.router.navigateByUrl('/login');
          return;
        }
        this.checking.set(false);
      },
      error: () => {
        // Gateway unreachable — still allow wizard UI for offline prep messaging.
        this.checking.set(false);
      },
    });
  }

  testDb(): void {
    this.busy.set(true);
    this.error.set('');
    this.api.testDatabase(this.connectionString).subscribe({
      next: (result) => {
        this.dbOk.set(result.success);
        this.dbMessage.set(result.message);
        this.busy.set(false);
      },
      error: (err) => {
        this.dbOk.set(false);
        this.dbMessage.set(err?.error?.message || 'Database test failed');
        this.busy.set(false);
      },
    });
  }

  saveConfig(): void {
    this.busy.set(true);
    this.error.set('');
    const payload: SetupConfigurePayload = {
      database: { connectionString: this.connectionString, provider: 'SqlServer' },
    };
    if (this.smtpHost.trim()) {
      payload.smtp = {
        host: this.smtpHost.trim(),
        port: this.smtpPort,
        fromAddress: this.smtpFrom || undefined,
        useSsl: this.smtpSsl,
      };
    }
    if (this.storageProvider.trim()) {
      payload.storage = {
        provider: this.storageProvider.trim(),
        rootPath: this.storageRoot || undefined,
      };
    }
    if (this.aiEndpoint.trim()) {
      payload.ai = {
        enabled: this.aiEnabled,
        endpoint: this.aiEndpoint.trim(),
      };
    } else if (!this.aiEnabled) {
      payload.ai = { enabled: false };
    }
    if (this.jwtIssuer.trim() || this.jwtAudience.trim() || this.jwtSecret.trim()) {
      payload.auth = {
        jwtIssuer: this.jwtIssuer.trim() || undefined,
        jwtAudience: this.jwtAudience.trim() || undefined,
        jwtSecretKey: this.jwtSecret.trim() || undefined,
      };
    }
    this.api.configure(payload).subscribe({
      next: (result) => {
        this.configSaved.set(result.saved);
        this.configMessage.set(`Saved to ${result.path}`);
        this.busy.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Failed to save configuration');
        this.busy.set(false);
      },
    });
  }

  finish(): void {
    this.busy.set(true);
    this.error.set('');
    this.api.complete().subscribe({
      next: (result) => {
        this.pings.set(result.healthPings ?? []);
        this.busy.set(false);
        void this.router.navigateByUrl('/login');
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Failed to complete installation');
        this.busy.set(false);
      },
    });
  }
}
