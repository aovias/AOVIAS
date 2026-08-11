import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { OrganizationService } from '../../../core/services/organization.service';
import { NotificationService } from '../../../core/ui/services/notification.service';
import { FileUploadComponent } from '../../../shared/ui/file-upload/file-upload.component';
import { SettingsPageHeaderComponent } from '../components/settings-page-header.component';
import { AdminPageStateComponent } from '../../../shared/ui/administration/admin-page-state.component';
import {
  DEFAULT_TIME_ZONE,
  OrganizationSettingsModel,
  SUPPORTED_LANGUAGES,
  WEEKDAYS,
} from '../models/organization-settings.model';
import { OrganizationSettingsApiService } from '../services/organization-settings-api.service';
import { SettingsDirtyComponent } from '../guards/settings-dirty.guard';

function organizationCodeValidator(control: AbstractControl): ValidationErrors | null {
  const value = (control.value as string | null)?.trim().toUpperCase() ?? '';
  if (!value) return { required: true };
  return /^[A-Z0-9_]+$/.test(value) ? null : { organizationCode: true };
}

function workingHoursValidator(group: AbstractControl): ValidationErrors | null {
  const start = group.get('start')?.value as string;
  const end = group.get('end')?.value as string;
  if (!start || !end) return null;
  return start < end ? null : { workingHours: true };
}

@Component({
  selector: 'app-organization-settings-page',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    FileUploadComponent,
    SettingsPageHeaderComponent,
    AdminPageStateComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="org-settings">
      <div class="org-settings__top">
        <app-settings-page-header
          title="Organization"
          description="Configure organization-wide platform settings used across AOVIAS Command."
        />
        @if (!canEdit()) {
          <span class="org-settings__readonly" role="status">Read-only</span>
        }
      </div>

      @if (loading()) {
        <app-admin-page-state
          state="loading"
          message="Loading organization settings…"
          [showRetry]="false"
        />
      } @else {
        <form [formGroup]="form" (ngSubmit)="save()" class="org-settings__form">
          <mat-card class="org-settings__section">
            <mat-card-header>
              <mat-card-title>Organization Identity</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="org-settings__grid">
                <mat-form-field appearance="outline">
                  <mat-label>Organization Name</mat-label>
                  <input matInput formControlName="organizationName" maxlength="200" />
                  @if (form.controls.organizationName.hasError('required')) {
                    <mat-error>Organization name is required</mat-error>
                  }
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Organization Code</mat-label>
                  <input
                    matInput
                    formControlName="organizationCode"
                    maxlength="20"
                    style="text-transform: uppercase"
                  />
                  <mat-hint>Uppercase letters, numbers, underscores only</mat-hint>
                  @if (form.controls.organizationCode.hasError('organizationCode')) {
                    <mat-error>Invalid organization code format</mat-error>
                  }
                </mat-form-field>
              </div>

              <div class="org-settings__logo">
                <div class="org-settings__logo-preview" aria-label="Organization logo preview">
                  @if (logoPreviewUrl()) {
                    <img [src]="logoPreviewUrl()" alt="Organization logo" />
                  } @else {
                    <mat-icon aria-hidden="true">business</mat-icon>
                    <span>No logo uploaded</span>
                  }
                </div>
                @if (canEdit()) {
                  <div class="org-settings__logo-actions">
                    <app-file-upload
                      accept="image/png,image/jpeg,image/svg+xml"
                      buttonText="Upload Logo"
                      (fileSelected)="onLogoSelected($event)"
                    />
                    @if (hasLogo()) {
                      <button
                        type="button"
                        mat-stroked-button
                        (click)="removeLogo()"
                        [disabled]="logoBusy()"
                      >
                        Remove Logo
                      </button>
                    }
                  </div>
                }
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="org-settings__section">
            <mat-card-header>
              <mat-card-title>Regional Configuration</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="org-settings__grid">
                <mat-form-field appearance="outline">
                  <mat-label>Time Zone</mat-label>
                  <mat-select formControlName="timeZoneId">
                    @for (tz of timeZones; track tz) {
                      <mat-option [value]="tz">{{ tz }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>

                <mat-form-field appearance="outline">
                  <mat-label>Language</mat-label>
                  <mat-select formControlName="languageCode">
                    @for (lang of languages; track lang.code) {
                      <mat-option [value]="lang.code">{{ lang.label }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="org-settings__section">
            <mat-card-header>
              <mat-card-title>Working Schedule</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <fieldset class="org-settings__days">
                <legend>Working Days</legend>
                <div class="org-settings__days-grid">
                  @for (day of weekdays; track day) {
                    <mat-checkbox
                      [checked]="isDaySelected(day)"
                      (change)="toggleDay(day, $event.checked)"
                      [disabled]="form.disabled"
                    >
                      {{ day }}
                    </mat-checkbox>
                  }
                </div>
                @if (selectedDays().length === 0) {
                  <p class="org-settings__error">Select at least one working day</p>
                }
              </fieldset>

              <div class="org-settings__grid" formGroupName="workingHours">
                <mat-form-field appearance="outline">
                  <mat-label>Start Time</mat-label>
                  <input matInput type="time" formControlName="start" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>End Time</mat-label>
                  <input matInput type="time" formControlName="end" />
                </mat-form-field>
              </div>
              @if (form.get('workingHours')?.hasError('workingHours')) {
                <p class="org-settings__error">End time must be after start time</p>
              }
            </mat-card-content>
          </mat-card>

          <section class="org-settings__future" aria-label="Related settings">
            <mat-card class="org-settings__section org-settings__section--link">
              <mat-card-header>
                <mat-card-title>Branding &amp; Appearance</mat-card-title>
                <mat-card-subtitle
                  >Logos, colors, reports, emails, and login presentation</mat-card-subtitle
                >
              </mat-card-header>
              <mat-card-content>
                <a mat-stroked-button routerLink="/app/settings/general/branding"
                  >Manage Branding</a
                >
              </mat-card-content>
            </mat-card>
            <mat-card class="org-settings__section org-settings__section--disabled">
              <mat-card-header>
                <mat-card-title>Regional Settings</mat-card-title>
                <mat-card-subtitle>Coming Soon</mat-card-subtitle>
              </mat-card-header>
            </mat-card>
          </section>

          @if (canEdit()) {
            <div class="org-settings__actions">
              <button
                mat-flat-button
                color="primary"
                type="submit"
                [disabled]="saving() || !form.valid || selectedDays().length === 0"
              >
                Save
              </button>
              <button
                mat-stroked-button
                type="button"
                (click)="cancel()"
                [disabled]="saving() || !dirty()"
              >
                Cancel
              </button>
            </div>
          }
        </form>
      }
    </div>
  `,
  styles: [
    `
      .org-settings__top {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 16px;
      }
      .org-settings__readonly {
        padding: 4px 10px;
        border-radius: 999px;
        background: #f3f4f6;
        color: #4b5563;
        font-size: 12px;
        font-weight: 600;
        flex-shrink: 0;
        margin-top: 8px;
      }
      .org-settings__section {
        margin-bottom: 20px;
      }
      .org-settings__section--disabled {
        opacity: 0.65;
        pointer-events: none;
      }
      .org-settings__section--link mat-card-content {
        padding-top: 0;
      }
      .org-settings__grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
        gap: 16px;
      }
      .org-settings__logo {
        display: flex;
        flex-wrap: wrap;
        gap: 16px;
        align-items: center;
        margin-top: 8px;
      }
      .org-settings__logo-preview {
        width: 120px;
        height: 120px;
        border: 1px dashed var(--color-border-subtle, #d1d5db);
        border-radius: 12px;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 8px;
        color: var(--color-text-secondary, #6b7280);
        overflow: hidden;
        background: #fff;
      }
      .org-settings__logo-preview img {
        width: 100%;
        height: 100%;
        object-fit: contain;
      }
      .org-settings__logo-actions {
        display: flex;
        flex-direction: column;
        gap: 8px;
      }
      .org-settings__days {
        border: 0;
        margin: 0 0 16px;
        padding: 0;
      }
      .org-settings__days legend {
        font-weight: 600;
        margin-bottom: 8px;
      }
      .org-settings__days-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
        gap: 4px 12px;
      }
      .org-settings__future {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        gap: 16px;
        margin-bottom: 24px;
      }
      .org-settings__actions {
        display: flex;
        gap: 12px;
        flex-wrap: wrap;
      }
      .org-settings__error {
        color: #b91c1c;
        font-size: 12px;
        margin: 4px 0 0;
      }
    `,
  ],
})
export class OrganizationSettingsPageComponent implements OnInit, SettingsDirtyComponent {
  private readonly api = inject(OrganizationSettingsApiService);
  private readonly organizationService = inject(OrganizationService);
  private readonly notify = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly weekdays = WEEKDAYS;
  readonly languages = SUPPORTED_LANGUAGES;
  readonly timeZones = this.buildTimeZoneList();

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly logoBusy = signal(false);
  readonly canEdit = signal(false);
  readonly dirty = signal(false);
  readonly hasLogo = signal(false);
  readonly logoPreviewUrl = signal<string | null>(null);
  readonly selectedDays = signal<string[]>([]);

  private snapshot: OrganizationSettingsModel | null = null;
  private logoCacheBust = Date.now();

  readonly form = this.fb.group({
    organizationName: ['', [Validators.required, Validators.maxLength(200)]],
    organizationCode: [
      '',
      [Validators.required, Validators.maxLength(20), organizationCodeValidator],
    ],
    timeZoneId: [DEFAULT_TIME_ZONE, Validators.required],
    languageCode: ['en', Validators.required],
    workingHours: this.fb.group(
      {
        start: ['09:00', Validators.required],
        end: ['18:00', Validators.required],
      },
      { validators: workingHoursValidator },
    ),
    rowVersion: [null as string | null],
  });

  ngOnInit(): void {
    this.form.valueChanges.subscribe(() => {
      this.dirty.set(this.form.dirty);
      this.cdr.markForCheck();
    });
    this.load();
  }

  isSettingsDirty(): boolean {
    return this.dirty();
  }

  isDaySelected(day: string): boolean {
    return this.selectedDays().includes(day);
  }

  toggleDay(day: string, checked: boolean): void {
    const next = checked
      ? [...this.selectedDays(), day]
      : this.selectedDays().filter((d) => d !== day);
    this.selectedDays.set(next);
    this.dirty.set(true);
    this.cdr.markForCheck();
  }

  onLogoSelected(files: FileList): void {
    const file = files.item(0);
    if (!file || !this.canEdit()) return;

    this.logoBusy.set(true);
    this.api.uploadLogo(file).subscribe({
      next: (settings) => {
        this.applySettings(settings);
        this.logoCacheBust = Date.now();
        this.updateLogoPreview(settings);
        this.notify.showSuccess('Logo uploaded');
        this.logoBusy.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.notify.showError('Logo upload failed');
        this.logoBusy.set(false);
        this.cdr.markForCheck();
      },
    });
  }

  removeLogo(): void {
    if (!this.canEdit()) return;
    this.logoBusy.set(true);
    this.api.removeLogo().subscribe({
      next: (settings) => {
        this.applySettings(settings);
        this.logoPreviewUrl.set(null);
        this.hasLogo.set(false);
        this.notify.showSuccess('Logo removed');
        this.logoBusy.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.notify.showError('Failed to remove logo');
        this.logoBusy.set(false);
        this.cdr.markForCheck();
      },
    });
  }

  save(): void {
    if (!this.canEdit() || this.form.invalid || this.selectedDays().length === 0) return;

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.api
      .updateSettings({
        organizationName: value.organizationName!.trim(),
        organizationCode: value.organizationCode!.trim().toUpperCase(),
        timeZoneId: value.timeZoneId!,
        languageCode: value.languageCode!,
        workingDays: this.selectedDays(),
        workingHours: {
          start: value.workingHours!.start!,
          end: value.workingHours!.end!,
        },
        rowVersion: value.rowVersion ?? null,
      })
      .subscribe({
        next: (settings) => {
          this.applySettings(settings);
          this.organizationService.invalidateCache();
          this.organizationService.loadOrganizationDetails(true).subscribe();
          this.notify.showSuccess('Organization settings saved');
          this.saving.set(false);
          this.dirty.set(false);
          this.form.markAsPristine();
          this.cdr.markForCheck();
        },
        error: () => {
          this.notify.showError('Failed to save organization settings');
          this.saving.set(false);
          this.cdr.markForCheck();
        },
      });
  }

  cancel(): void {
    if (!this.snapshot) return;
    this.applySettings(this.snapshot);
    this.dirty.set(false);
    this.form.markAsPristine();
    this.cdr.markForCheck();
  }

  private load(): void {
    this.loading.set(true);
    this.api.getSettings().subscribe({
      next: (settings) => {
        this.applySettings(settings);
        this.loading.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.notify.showError('Failed to load organization settings');
        this.loading.set(false);
        this.cdr.markForCheck();
      },
    });
  }

  private applySettings(settings: OrganizationSettingsModel): void {
    this.snapshot = settings;
    this.canEdit.set(settings.canEdit);
    this.selectedDays.set([...settings.workingDays]);
    this.form.reset({
      organizationName: settings.organizationName,
      organizationCode: settings.organizationCode,
      timeZoneId: settings.timeZoneId,
      languageCode: settings.languageCode,
      workingHours: {
        start: settings.workingHours.start,
        end: settings.workingHours.end,
      },
      rowVersion: settings.rowVersion,
    });
    if (!settings.canEdit) {
      this.form.disable();
    } else {
      this.form.enable();
    }
    this.updateLogoPreview(settings);
    this.dirty.set(false);
    this.form.markAsPristine();
  }

  private updateLogoPreview(settings: OrganizationSettingsModel): void {
    const has = !!settings.logoUrl;
    this.hasLogo.set(has);
    this.logoPreviewUrl.set(has ? this.api.logoPreviewUrl(this.logoCacheBust) : null);
  }

  private buildTimeZoneList(): string[] {
    try {
      const intl = Intl as unknown as { supportedValuesOf?: (key: string) => string[] };
      if (typeof intl.supportedValuesOf === 'function') {
        return intl.supportedValuesOf('timeZone').sort();
      }
    } catch {
      /* fallback below */
    }
    return [
      'Asia/Kolkata',
      'UTC',
      'America/New_York',
      'America/Los_Angeles',
      'Europe/London',
      'Europe/Berlin',
      'Asia/Singapore',
      'Australia/Sydney',
    ];
  }
}
