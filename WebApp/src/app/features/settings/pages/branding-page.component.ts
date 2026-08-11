import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { NotificationService } from '../../../core/ui/services/notification.service';
import { FileUploadComponent } from '../../../shared/ui/file-upload/file-upload.component';
import { SettingsPageHeaderComponent } from '../components/settings-page-header.component';
import { SettingsDirtyComponent } from '../guards/settings-dirty.guard';
import {
  BRANDING_ASSET_LABELS,
  BRANDING_IMAGE_ACCEPT,
  BrandingAssetKey,
  BrandingAssetUrlsModel,
  BrandingConfigModel,
  OrganizationBrandingModel,
} from '../models/organization-branding.model';
import { OrganizationBrandingApiService } from '../services/organization-branding-api.service';
import { OrganizationBrandingThemeService } from '../../../core/services/organization-branding-theme.service';

const MAX_FILE_BYTES = 2 * 1024 * 1024;
const ALLOWED_TYPES = new Set(['image/png', 'image/jpeg', 'image/svg+xml']);

type BrandingAssetSlot = BrandingAssetKey | 'primary';

const ASSET_SLOTS: Array<{ key: BrandingAssetSlot; optional?: boolean }> = [
  { key: 'primary' },
  { key: 'secondary', optional: true },
  { key: 'favicon' },
  { key: 'report' },
  { key: 'emailHeader' },
  { key: 'emailFooter' },
  { key: 'loginBackground' },
];

const ASSET_URL_FIELD: Record<BrandingAssetSlot, keyof BrandingAssetUrlsModel> = {
  primary: 'primaryLogoUrl',
  secondary: 'secondaryLogoUrl',
  favicon: 'faviconUrl',
  report: 'reportLogoUrl',
  emailHeader: 'emailHeaderLogoUrl',
  emailFooter: 'emailFooterLogoUrl',
  loginBackground: 'loginBackgroundUrl',
};

const FUTURE_FEATURES = [
  'Theme Packs',
  'Custom Fonts',
  'Dark Theme Branding',
  'Partner Branding',
  'White Label',
];

function hexColorValidator(control: { value: string | null }) {
  const value = (control.value ?? '').trim();
  if (!value) return { required: true };
  return /^#[0-9A-Fa-f]{6}$/.test(value) ? null : { hexColor: true };
}

@Component({
  selector: 'app-branding-page',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    FileUploadComponent,
    SettingsPageHeaderComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="branding-page">
      <div class="branding-page__top">
        <app-settings-page-header
          title="Branding & Appearance"
          description="Manage organization branding, logos, colors and presentation used throughout AOVIAS Command."
        />
        @if (!canEdit()) {
          <span class="branding-page__readonly" role="status">Read-only</span>
        }
      </div>

      @if (loading()) {
        <div class="branding-page__loading" aria-live="polite">
          <mat-spinner diameter="40" />
          <span>Loading branding configuration…</span>
        </div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="save()" class="branding-page__form">
          <mat-card class="branding-page__section">
            <mat-card-header>
              <mat-card-title>Logos &amp; Assets</mat-card-title>
              <mat-card-subtitle>PNG, JPG, or SVG — maximum 2 MB per file</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <div class="branding-page__assets">
                @for (slot of assetSlots; track slot.key) {
                  <div class="branding-page__asset">
                    <div class="branding-page__asset-header">
                      <span class="branding-page__asset-label">
                        {{ assetLabels[slot.key] }}
                        @if (slot.optional) {
                          <span class="branding-page__optional">(optional)</span>
                        }
                      </span>
                      @if (assetBusy()[slot.key]) {
                        <mat-spinner diameter="18" />
                      }
                    </div>
                    <div
                      class="branding-page__asset-preview"
                      [class.branding-page__asset-preview--wide]="slot.key === 'loginBackground'"
                      [attr.aria-label]="assetLabels[slot.key] + ' preview'"
                    >
                      @if (hasAsset(slot.key)) {
                        <img [src]="previewUrl(slot.key)" [alt]="assetLabels[slot.key]" />
                      } @else {
                        <mat-icon aria-hidden="true">image</mat-icon>
                        <span>No image uploaded</span>
                      }
                    </div>
                    @if (canEdit()) {
                      <div class="branding-page__asset-actions">
                        <app-file-upload
                          [accept]="imageAccept"
                          buttonText="Upload"
                          (fileSelected)="onAssetSelected(slot.key, $event)"
                        />
                        @if (hasAsset(slot.key)) {
                          <button
                            type="button"
                            mat-stroked-button
                            (click)="removeAsset(slot.key)"
                            [disabled]="assetBusy()[slot.key]"
                          >
                            Remove
                          </button>
                        }
                      </div>
                    }
                  </div>
                }
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="branding-page__section" formGroupName="colors">
            <mat-card-header>
              <mat-card-title>Color Palette</mat-card-title>
              <mat-card-subtitle>
                Applied to your logo and to generated documents — reports, exports, and emails. The
                application interface always uses the AOVIAS accent.
              </mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <div class="branding-page__colors">
                @for (colorKey of colorKeys; track colorKey) {
                  <div class="branding-page__color">
                    <label [for]="'color-' + colorKey">{{ colorLabels[colorKey] }}</label>
                    <div class="branding-page__color-inputs">
                      <input
                        type="color"
                        [id]="'color-' + colorKey"
                        [formControlName]="colorKey"
                        [disabled]="form.disabled"
                      />
                      <mat-form-field appearance="outline" class="branding-page__hex-field">
                        <mat-label>Hex</mat-label>
                        <input matInput [formControlName]="colorKey" maxlength="7" />
                        @if (form.get('colors.' + colorKey)?.hasError('hexColor')) {
                          <mat-error>Enter a valid hex color (e.g. #2f6bff)</mat-error>
                        }
                      </mat-form-field>
                    </div>
                  </div>
                }
              </div>
              <div class="branding-page__palette-preview" aria-label="Color palette preview">
                <div class="branding-page__swatch" [style.background]="form.value.colors?.primary">
                  Primary
                </div>
                <div
                  class="branding-page__swatch"
                  [style.background]="form.value.colors?.secondary"
                >
                  Secondary
                </div>
                <div class="branding-page__swatch" [style.background]="form.value.colors?.accent">
                  Accent
                </div>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="branding-page__section" formGroupName="report">
            <mat-card-header>
              <mat-card-title>Report Branding</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="branding-page__grid">
                <mat-form-field appearance="outline">
                  <mat-label>Report Header</mat-label>
                  <textarea matInput formControlName="header" rows="2" maxlength="500"></textarea>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Report Footer</mat-label>
                  <textarea matInput formControlName="footer" rows="2" maxlength="500"></textarea>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Organization Motto</mat-label>
                  <input matInput formControlName="motto" maxlength="200" />
                  <mat-hint>Optional</mat-hint>
                </mat-form-field>
              </div>
              <div class="branding-page__watermark-placeholder" aria-label="Watermark placeholder">
                <mat-icon aria-hidden="true">water_drop</mat-icon>
                <span>Watermark — future placeholder</span>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="branding-page__section" formGroupName="email">
            <mat-card-header>
              <mat-card-title>Email Branding</mat-card-title>
              <mat-card-subtitle>Supports future template integration</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <div class="branding-page__grid">
                <mat-form-field appearance="outline">
                  <mat-label>Email Header</mat-label>
                  <textarea matInput formControlName="header" rows="2" maxlength="500"></textarea>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Email Footer</mat-label>
                  <textarea matInput formControlName="footer" rows="2" maxlength="500"></textarea>
                </mat-form-field>
                <mat-form-field appearance="outline" class="branding-page__full-width">
                  <mat-label>Signature</mat-label>
                  <textarea
                    matInput
                    formControlName="signature"
                    rows="3"
                    maxlength="1000"
                  ></textarea>
                </mat-form-field>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="branding-page__section" formGroupName="login">
            <mat-card-header>
              <mat-card-title>Login Branding</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="branding-page__grid">
                <mat-form-field appearance="outline">
                  <mat-label>Welcome Message</mat-label>
                  <input matInput formControlName="welcomeMessage" maxlength="200" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Organization Tagline</mat-label>
                  <input matInput formControlName="tagline" maxlength="200" />
                </mat-form-field>
              </div>
              <div
                class="branding-page__watermark-placeholder"
                aria-label="Illustration placeholder"
              >
                <mat-icon aria-hidden="true">draw</mat-icon>
                <span>Illustration — future placeholder</span>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="branding-page__section">
            <mat-card-header>
              <mat-card-title>Live Preview</mat-card-title>
              <mat-card-subtitle
                >Mock previews — not connected to runtime theming</mat-card-subtitle
              >
            </mat-card-header>
            <mat-card-content>
              <div class="branding-page__previews">
                <div class="branding-page__preview-card" aria-label="Application preview">
                  <span class="branding-page__preview-label">Application</span>
                  <div class="branding-page__preview-app">
                    <div
                      class="branding-page__preview-app-bar"
                      [style.background]="form.value.colors?.primary"
                    >
                      @if (hasAsset('primary')) {
                        <img [src]="previewUrl('primary')" alt="" />
                      }
                      <span>AOVIAS Command</span>
                    </div>
                    <div class="branding-page__preview-app-body">
                      <div
                        class="branding-page__preview-chip"
                        [style.background]="form.value.colors?.accent"
                      >
                        Accent
                      </div>
                      <div class="branding-page__preview-line"></div>
                      <div
                        class="branding-page__preview-line branding-page__preview-line--short"
                      ></div>
                    </div>
                  </div>
                </div>

                <div class="branding-page__preview-card" aria-label="Report preview">
                  <span class="branding-page__preview-label">Report</span>
                  <div class="branding-page__preview-report">
                    <header [style.borderColor]="form.value.colors?.primary">
                      @if (hasAsset('report') || hasAsset('primary')) {
                        <img
                          [src]="hasAsset('report') ? previewUrl('report') : previewUrl('primary')"
                          alt=""
                        />
                      }
                      <div>
                        <strong>{{ form.value.report?.header || 'Report Header' }}</strong>
                        @if (form.value.report?.motto) {
                          <em>{{ form.value.report?.motto }}</em>
                        }
                      </div>
                    </header>
                    <div class="branding-page__preview-report-body">
                      <div class="branding-page__preview-line"></div>
                      <div class="branding-page__preview-line"></div>
                    </div>
                    <footer>{{ form.value.report?.footer || 'Report Footer' }}</footer>
                  </div>
                </div>

                <div class="branding-page__preview-card" aria-label="Email preview">
                  <span class="branding-page__preview-label">Email</span>
                  <div class="branding-page__preview-email">
                    @if (hasAsset('emailHeader')) {
                      <img
                        class="branding-page__preview-email-banner"
                        [src]="previewUrl('emailHeader')"
                        alt=""
                      />
                    }
                    <p class="branding-page__preview-email-header">
                      {{ form.value.email?.header || 'Email Header' }}
                    </p>
                    <p>Sample notification content for your organization.</p>
                    <p class="branding-page__preview-email-signature">
                      {{ form.value.email?.signature || 'Signature' }}
                    </p>
                    <footer>
                      @if (hasAsset('emailFooter')) {
                        <img [src]="previewUrl('emailFooter')" alt="" />
                      }
                      {{ form.value.email?.footer || 'Email Footer' }}
                    </footer>
                  </div>
                </div>

                <div class="branding-page__preview-card" aria-label="Login preview">
                  <span class="branding-page__preview-label">Login</span>
                  <div
                    class="branding-page__preview-login"
                    [style.backgroundImage]="
                      hasAsset('loginBackground')
                        ? 'url(' + previewUrl('loginBackground') + ')'
                        : null
                    "
                  >
                    <div class="branding-page__preview-login-panel">
                      @if (hasAsset('primary')) {
                        <img [src]="previewUrl('primary')" alt="" />
                      }
                      <h3>{{ form.value.login?.welcomeMessage || 'Welcome' }}</h3>
                      <p>{{ form.value.login?.tagline || 'Organization tagline' }}</p>
                      <div class="branding-page__preview-login-field"></div>
                      <div
                        class="branding-page__preview-login-btn"
                        [style.background]="form.value.colors?.primary"
                      >
                        Sign in
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </mat-card-content>
          </mat-card>

          <section class="branding-page__future" aria-label="Future branding features">
            @for (feature of futureFeatures; track feature) {
              <mat-card class="branding-page__section branding-page__section--disabled">
                <mat-card-header>
                  <mat-card-title>{{ feature }}</mat-card-title>
                  <mat-card-subtitle>Coming Soon</mat-card-subtitle>
                </mat-card-header>
              </mat-card>
            }
          </section>

          @if (canEdit()) {
            <div class="branding-page__actions">
              <button
                mat-flat-button
                color="primary"
                type="submit"
                [disabled]="saving() || !form.valid || !dirty()"
              >
                Save Changes
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
      .branding-page__top {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 16px;
      }
      .branding-page__readonly {
        padding: 4px 10px;
        border-radius: 999px;
        background: #f3f4f6;
        color: #4b5563;
        font-size: 12px;
        font-weight: 600;
        flex-shrink: 0;
        margin-top: 8px;
      }
      .branding-page__loading {
        display: flex;
        align-items: center;
        gap: 16px;
        padding: 48px 0;
        color: var(--color-text-secondary, #6b7280);
      }
      .branding-page__form {
        display: flex;
        flex-direction: column;
        gap: 20px;
      }
      .branding-page__section {
        border-radius: 16px;
        border: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .branding-page__section--disabled {
        opacity: 0.65;
      }
      .branding-page__assets {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
        gap: 20px;
      }
      .branding-page__asset {
        display: flex;
        flex-direction: column;
        gap: 10px;
      }
      .branding-page__asset-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 8px;
      }
      .branding-page__asset-label {
        font-size: 14px;
        font-weight: 600;
      }
      .branding-page__optional {
        font-weight: 400;
        color: var(--color-text-secondary, #6b7280);
      }
      .branding-page__asset-preview {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 8px;
        min-height: 120px;
        border: 1px dashed var(--color-border-subtle, #d1d5db);
        border-radius: 12px;
        background: #f9fafb;
        padding: 12px;
        color: var(--color-text-secondary, #6b7280);
        font-size: 12px;
        overflow: hidden;
      }
      .branding-page__asset-preview--wide {
        min-height: 100px;
      }
      .branding-page__asset-preview img {
        max-width: 100%;
        max-height: 96px;
        object-fit: contain;
      }
      .branding-page__asset-preview--wide img {
        max-height: 80px;
        width: 100%;
        object-fit: cover;
        border-radius: 8px;
      }
      .branding-page__asset-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 8px;
        align-items: center;
      }
      .branding-page__colors {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
        gap: 16px;
        margin-bottom: 20px;
      }
      .branding-page__color label {
        display: block;
        font-size: 13px;
        font-weight: 600;
        margin-bottom: 8px;
      }
      .branding-page__color-inputs {
        display: flex;
        align-items: center;
        gap: 12px;
      }
      .branding-page__color-inputs input[type='color'] {
        width: 48px;
        height: 40px;
        border: none;
        padding: 0;
        background: transparent;
        cursor: pointer;
      }
      .branding-page__hex-field {
        flex: 1;
      }
      .branding-page__palette-preview {
        display: flex;
        gap: 12px;
        flex-wrap: wrap;
      }
      .branding-page__swatch {
        flex: 1;
        min-width: 120px;
        padding: 20px 16px;
        border-radius: 12px;
        color: #fff;
        font-size: 13px;
        font-weight: 600;
        text-shadow: 0 1px 2px rgba(0, 0, 0, 0.25);
      }
      .branding-page__grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
        gap: 16px;
      }
      .branding-page__full-width {
        grid-column: 1 / -1;
      }
      .branding-page__watermark-placeholder {
        display: flex;
        align-items: center;
        gap: 8px;
        margin-top: 12px;
        padding: 12px 16px;
        border-radius: 10px;
        background: #f3f4f6;
        color: #6b7280;
        font-size: 13px;
      }
      .branding-page__previews {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
        gap: 16px;
      }
      .branding-page__preview-card {
        border: 1px solid var(--color-border-subtle, #e5e7eb);
        border-radius: 12px;
        overflow: hidden;
        background: #fff;
      }
      .branding-page__preview-label {
        display: block;
        padding: 8px 12px;
        font-size: 12px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        background: #f9fafb;
        border-bottom: 1px solid var(--color-border-subtle, #e5e7eb);
      }
      .branding-page__preview-app-bar {
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 10px 12px;
        color: #fff;
        font-size: 13px;
        font-weight: 600;
      }
      .branding-page__preview-app-bar img {
        height: 20px;
        max-width: 80px;
        object-fit: contain;
      }
      .branding-page__preview-app-body {
        padding: 12px;
      }
      .branding-page__preview-chip {
        display: inline-block;
        padding: 2px 8px;
        border-radius: 999px;
        color: #fff;
        font-size: 11px;
        margin-bottom: 10px;
      }
      .branding-page__preview-line {
        height: 8px;
        background: #e5e7eb;
        border-radius: 4px;
        margin-bottom: 8px;
      }
      .branding-page__preview-line--short {
        width: 60%;
      }
      .branding-page__preview-report header {
        display: flex;
        gap: 10px;
        align-items: center;
        padding: 12px;
        border-bottom: 2px solid;
      }
      .branding-page__preview-report header img {
        height: 28px;
        max-width: 80px;
        object-fit: contain;
      }
      .branding-page__preview-report header strong {
        display: block;
        font-size: 13px;
      }
      .branding-page__preview-report header em {
        font-size: 11px;
        color: #6b7280;
      }
      .branding-page__preview-report-body {
        padding: 12px;
      }
      .branding-page__preview-report footer {
        padding: 8px 12px;
        font-size: 11px;
        color: #6b7280;
        border-top: 1px solid #e5e7eb;
      }
      .branding-page__preview-email {
        padding: 12px;
        font-size: 12px;
        line-height: 1.5;
      }
      .branding-page__preview-email-banner {
        width: 100%;
        max-height: 48px;
        object-fit: cover;
        border-radius: 6px;
        margin-bottom: 8px;
      }
      .branding-page__preview-email-header {
        font-weight: 600;
        margin: 0 0 8px;
      }
      .branding-page__preview-email-signature {
        margin: 12px 0;
        color: #4b5563;
        white-space: pre-wrap;
      }
      .branding-page__preview-email footer {
        display: flex;
        flex-direction: column;
        gap: 6px;
        margin-top: 12px;
        padding-top: 8px;
        border-top: 1px solid #e5e7eb;
        font-size: 11px;
        color: #6b7280;
      }
      .branding-page__preview-email footer img {
        max-height: 24px;
        max-width: 120px;
        object-fit: contain;
      }
      .branding-page__preview-login {
        min-height: 180px;
        background: linear-gradient(135deg, var(--color-primary-100, #d6e4ff), #f8fafc);
        background-size: cover;
        background-position: center;
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 16px;
      }
      .branding-page__preview-login-panel {
        background: #fff;
        border-radius: 12px;
        padding: 16px;
        width: 100%;
        max-width: 200px;
        text-align: center;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
      }
      .branding-page__preview-login-panel img {
        max-height: 32px;
        max-width: 100%;
        object-fit: contain;
        margin-bottom: 8px;
      }
      .branding-page__preview-login-panel h3 {
        margin: 0 0 4px;
        font-size: 14px;
      }
      .branding-page__preview-login-panel p {
        margin: 0 0 12px;
        font-size: 11px;
        color: #6b7280;
      }
      .branding-page__preview-login-field {
        height: 24px;
        background: #f3f4f6;
        border-radius: 6px;
        margin-bottom: 8px;
      }
      .branding-page__preview-login-btn {
        padding: 6px;
        border-radius: 6px;
        color: #fff;
        font-size: 12px;
        font-weight: 600;
      }
      .branding-page__future {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
        gap: 12px;
      }
      .branding-page__actions {
        display: flex;
        gap: 12px;
        flex-wrap: wrap;
        padding-bottom: 24px;
      }
      @media (max-width: 640px) {
        .branding-page__top {
          flex-direction: column;
        }
      }
    `,
  ],
})
export class BrandingPageComponent implements OnInit, SettingsDirtyComponent {
  private readonly api = inject(OrganizationBrandingApiService);
  private readonly brandingTheme = inject(OrganizationBrandingThemeService);
  private readonly notify = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly assetSlots = ASSET_SLOTS;
  readonly assetLabels = BRANDING_ASSET_LABELS;
  readonly imageAccept = BRANDING_IMAGE_ACCEPT;
  readonly futureFeatures = FUTURE_FEATURES;
  readonly colorKeys = ['primary', 'secondary', 'accent'] as const;
  readonly colorLabels: Record<(typeof this.colorKeys)[number], string> = {
    primary: 'Primary Color',
    secondary: 'Secondary Color',
    accent: 'Accent Color',
  };

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly canEdit = signal(false);
  readonly dirty = signal(false);
  readonly assetBusy = signal<Record<BrandingAssetSlot, boolean>>(this.emptyAssetBusy());

  private snapshot: OrganizationBrandingModel | null = null;
  private cacheBust = Date.now();

  readonly form = this.fb.group({
    colors: this.fb.group({
      primary: ['#2f6bff', [Validators.required, hexColorValidator]],
      secondary: ['#1E293B', [Validators.required, hexColorValidator]],
      accent: ['#0EA5E9', [Validators.required, hexColorValidator]],
    }),
    report: this.fb.group({
      header: ['', Validators.maxLength(500)],
      footer: ['', Validators.maxLength(500)],
      motto: ['', Validators.maxLength(200)],
    }),
    email: this.fb.group({
      header: ['', Validators.maxLength(500)],
      footer: ['', Validators.maxLength(500)],
      signature: ['', Validators.maxLength(1000)],
    }),
    login: this.fb.group({
      welcomeMessage: ['', Validators.maxLength(200)],
      tagline: ['', Validators.maxLength(200)],
    }),
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

  hasAsset(key: BrandingAssetSlot): boolean {
    const assets = this.snapshot?.assets;
    if (!assets) return false;
    return !!assets[ASSET_URL_FIELD[key]];
  }

  previewUrl(key: BrandingAssetSlot): string {
    return this.api.assetPreviewUrl(key, this.cacheBust);
  }

  onAssetSelected(key: BrandingAssetSlot, files: FileList): void {
    const file = files.item(0);
    if (!file || !this.canEdit()) return;
    if (!this.validateFile(file)) return;

    this.setAssetBusy(key, true);
    const request$ =
      key === 'primary' ? this.api.uploadPrimaryLogo(file) : this.api.uploadAsset(key, file);

    request$.subscribe({
      next: (branding) => {
        this.applyBranding(branding);
        this.cacheBust = Date.now();
        this.notify.showSuccess(`${BRANDING_ASSET_LABELS[key]} uploaded`);
        this.setAssetBusy(key, false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.notify.showError(`${BRANDING_ASSET_LABELS[key]} upload failed`);
        this.setAssetBusy(key, false);
        this.cdr.markForCheck();
      },
    });
  }

  removeAsset(key: BrandingAssetSlot): void {
    if (!this.canEdit() || !this.hasAsset(key)) return;

    this.setAssetBusy(key, true);
    const request$ = key === 'primary' ? this.api.removePrimaryLogo() : this.api.removeAsset(key);

    request$.subscribe({
      next: (branding) => {
        this.applyBranding(branding);
        this.cacheBust = Date.now();
        this.notify.showSuccess(`${BRANDING_ASSET_LABELS[key]} removed`);
        this.setAssetBusy(key, false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.notify.showError(`Failed to remove ${BRANDING_ASSET_LABELS[key]}`);
        this.setAssetBusy(key, false);
        this.cdr.markForCheck();
      },
    });
  }

  save(): void {
    if (!this.canEdit() || this.form.invalid) return;

    this.saving.set(true);
    const config = this.form.getRawValue() as BrandingConfigModel;
    this.api.updateBranding(config, this.snapshot?.rowVersion ?? null).subscribe({
      next: (branding) => {
        this.applyBranding(branding);
        this.form.markAsPristine();
        this.dirty.set(false);
        this.notify.showSuccess('Branding saved');
        this.saving.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.notify.showError('Failed to save branding');
        this.saving.set(false);
        this.cdr.markForCheck();
      },
    });
  }

  cancel(): void {
    if (!this.snapshot) return;
    this.patchForm(this.snapshot.config);
    this.form.markAsPristine();
    this.dirty.set(false);
    this.cdr.markForCheck();
  }

  private load(): void {
    this.loading.set(true);
    this.api.getBranding().subscribe({
      next: (branding) => {
        this.applyBranding(branding);
        this.loading.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.notify.showError('Failed to load branding');
        this.loading.set(false);
        this.cdr.markForCheck();
      },
    });
  }

  private applyBranding(branding: OrganizationBrandingModel): void {
    this.snapshot = branding;
    this.canEdit.set(branding.canEdit);
    this.patchForm(branding.config);
    this.brandingTheme.applyColors(branding.config.colors);
    if (!branding.canEdit) {
      this.form.disable({ emitEvent: false });
    } else {
      this.form.enable({ emitEvent: false });
    }
    this.form.markAsPristine();
    this.dirty.set(false);
  }

  private patchForm(config: BrandingConfigModel): void {
    this.form.patchValue(
      {
        colors: { ...config.colors },
        report: { ...config.report },
        email: { ...config.email },
        login: { ...config.login },
      },
      { emitEvent: false },
    );
  }

  private validateFile(file: File): boolean {
    if (!ALLOWED_TYPES.has(file.type)) {
      this.notify.showError('Only PNG, JPG, and SVG images are allowed');
      return false;
    }
    if (file.size > MAX_FILE_BYTES) {
      this.notify.showError('Image must be 2 MB or smaller');
      return false;
    }
    return true;
  }

  private setAssetBusy(key: BrandingAssetSlot, busy: boolean): void {
    this.assetBusy.update((state) => ({ ...state, [key]: busy }));
  }

  private emptyAssetBusy(): Record<BrandingAssetSlot, boolean> {
    return {
      primary: false,
      secondary: false,
      favicon: false,
      report: false,
      emailHeader: false,
      emailFooter: false,
      loginBackground: false,
    };
  }
}
