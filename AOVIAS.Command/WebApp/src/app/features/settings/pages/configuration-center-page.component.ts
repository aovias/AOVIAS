import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { ConfigurationAdminService } from '../../../core/configuration/configuration-admin.service';
import type { ConfigurationValueDescriptor } from '../../../core/configuration/configuration-catalog';

/**
 * The Configuration Center — one editor for every configuration domain.
 *
 * <p>
 * **One page, not one per domain.** Every category is the same shape: an ordered list of coded
 * values with a label, an active flag and a bag of attributes. Giving Risk, Approval, Notification,
 * SLA, Evidence, Document and Dashboard their own pages would be seven copies of the same CRUD,
 * each free to drift — which is the failure this whole programme has been removing. The category
 * list on the left is generated from the registry, so a domain added to a catalog module appears
 * here with no work at all.
 * </p>
 *
 * <p>
 * **Attributes are edited as JSON, deliberately.** Attributes are domain-specific — `minScore` on a
 * risk level, `completeWhen` on a workflow stage, `retentionMonths` on a policy — and a per-attribute
 * form would need a schema per category, which is the per-domain code this page exists to avoid. The
 * trade is real and stated: this is an administrator's tool, not an end-user's, and malformed JSON
 * is rejected before it can be sent.
 * </p>
 */
@Component({
  selector: 'app-configuration-center-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="cfg">
      <header class="cfg__head">
        <div>
          <h1>Configuration Center</h1>
          <p class="cfg__sub">
            Business behaviour across the platform. Changes take effect immediately for everyone in
            this organization — no release required.
          </p>
        </div>
      </header>

      @if (admin.error(); as message) {
        <div class="cfg__banner" role="alert">
          {{ message }}
          <button type="button" class="cfg__banner-close" (click)="admin.clearError()">
            Dismiss
          </button>
        </div>
      }

      <div class="cfg__body">
        <nav class="cfg__list" aria-label="Configuration domains">
          @for (category of admin.categories(); track category.code) {
            <a
              class="cfg__item"
              [class.cfg__item--active]="category.code === activeCode()"
              [routerLink]="[]"
              [queryParams]="{ category: category.code }"
            >
              <span class="cfg__item-name">{{ category.code }}</span>
              <span class="cfg__item-meta">
                {{ category.valueCount }} value{{ category.valueCount === 1 ? '' : 's' }}
                @if (category.isOverridden) {
                  <span class="cfg__badge">Customised</span>
                }
              </span>
            </a>
          }
        </nav>

        <div class="cfg__detail">
          @if (activeCategory(); as category) {
            <h2>{{ category.code }}</h2>
            <p class="cfg__desc">{{ category.description }}</p>

            <table class="cfg__table">
              <thead>
                <tr>
                  <th scope="col">Order</th>
                  <th scope="col">Code</th>
                  <th scope="col">Label</th>
                  <th scope="col">Attributes</th>
                  <th scope="col"><span class="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody>
                @for (value of values(); track value.id) {
                  <tr [class.cfg__row--editing]="editing() === value.id">
                    @if (editing() === value.id) {
                      <td><input class="cfg__num" type="number" [(ngModel)]="draftSortOrder" /></td>
                      <td class="cfg__code">{{ value.id }}</td>
                      <td><input class="cfg__text" [(ngModel)]="draftLabel" /></td>
                      <td>
                        <textarea
                          class="cfg__json"
                          rows="3"
                          [ngModel]="draftAttributes"
                          (ngModelChange)="onAttributesChanged($event)"
                          [attr.aria-invalid]="attributeError() ? 'true' : null"
                        ></textarea>
                        @if (attributeError(); as error) {
                          <p class="cfg__error">{{ error }}</p>
                        }
                      </td>
                      <td class="cfg__actions">
                        <button
                          type="button"
                          class="cfg__save"
                          [disabled]="!!attributeError() || admin.saving() === value.id"
                          (click)="commit(category.code, value)"
                        >
                          Save
                        </button>
                        <button type="button" class="cfg__cancel" (click)="cancel()">Cancel</button>
                      </td>
                    } @else {
                      <td class="cfg__num-read">{{ value.sortOrder }}</td>
                      <td class="cfg__code">{{ value.id }}</td>
                      <td>{{ value.label }}</td>
                      <td class="cfg__attrs">{{ describeAttributes(value) }}</td>
                      <td class="cfg__actions">
                        <button type="button" class="cfg__edit" (click)="edit(value)">Edit</button>
                        <button
                          type="button"
                          class="cfg__restore"
                          [disabled]="admin.saving() === value.id"
                          (click)="restore(category.code, value.id)"
                        >
                          Restore default
                        </button>
                      </td>
                    }
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="5" class="cfg__empty">
                      This category has no values. Its platform default is defined in code rather
                      than as rows.
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          } @else {
            <p class="cfg__empty">Select a domain to configure.</p>
          }
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .cfg {
        display: block;
        padding: 16px;
      }
      .cfg__head {
        margin-bottom: 16px;
      }
      h1 {
        margin: 0 0 4px;
        font-size: 20px;
      }
      .cfg__sub,
      .cfg__desc {
        margin: 0;
        color: var(--text-secondary);
        font-size: 13px;
      }
      .cfg__banner {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        margin-bottom: 12px;
        padding: 10px 12px;
        border-radius: 8px;
        background: color-mix(in srgb, var(--color-danger, #dc2626) 12%, transparent);
        color: var(--color-danger, #dc2626);
        font-size: 13px;
      }
      .cfg__banner-close {
        border: 0;
        background: none;
        color: inherit;
        text-decoration: underline;
        cursor: pointer;
      }
      .cfg__body {
        display: grid;
        grid-template-columns: minmax(200px, 260px) 1fr;
        gap: 16px;
        align-items: start;
      }
      .cfg__list {
        display: flex;
        flex-direction: column;
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        overflow: hidden;
        max-height: 70vh;
        overflow-y: auto;
      }
      .cfg__item {
        display: flex;
        flex-direction: column;
        gap: 2px;
        padding: 8px 12px;
        border-bottom: 1px solid var(--border-color, #e5e7eb);
        text-decoration: none;
        color: var(--text-primary);
      }
      .cfg__item--active {
        background: var(--color-primary-soft, #eff6ff);
        font-weight: 600;
      }
      .cfg__item-name {
        font-size: 13px;
      }
      .cfg__item-meta {
        font-size: 11px;
        color: var(--text-secondary);
      }
      .cfg__badge {
        margin-left: 6px;
        padding: 1px 6px;
        border-radius: 999px;
        background: var(--color-primary-soft, #eff6ff);
        color: var(--color-primary, #2563eb);
        font-size: 10px;
      }
      .cfg__detail {
        border: 1px solid var(--border-color, #e5e7eb);
        border-radius: 8px;
        padding: 12px;
        min-width: 0;
        overflow-x: auto;
      }
      h2 {
        margin: 0 0 2px;
        font-size: 15px;
      }
      .cfg__table {
        width: 100%;
        margin-top: 12px;
        border-collapse: collapse;
        font-size: 12px;
      }
      th,
      td {
        padding: 6px 8px;
        text-align: left;
        border-bottom: 1px solid var(--border-color, #f0f0f0);
        vertical-align: top;
      }
      th {
        font-size: 11px;
        text-transform: uppercase;
        color: var(--text-secondary);
      }
      .cfg__code {
        font-family: ui-monospace, monospace;
      }
      .cfg__attrs {
        font-family: ui-monospace, monospace;
        color: var(--text-secondary);
        max-width: 320px;
        word-break: break-word;
      }
      .cfg__num,
      .cfg__num-read {
        width: 64px;
      }
      .cfg__text,
      .cfg__json,
      .cfg__num {
        width: 100%;
        padding: 4px 6px;
        border: 1px solid var(--border-color, #d1d5db);
        border-radius: 4px;
        font: inherit;
        background: var(--input-bg, transparent);
        color: inherit;
      }
      .cfg__json {
        font-family: ui-monospace, monospace;
        min-width: 260px;
      }
      .cfg__error {
        margin: 4px 0 0;
        color: var(--color-danger, #dc2626);
        font-size: 11px;
      }
      .cfg__actions {
        display: flex;
        gap: 6px;
        white-space: nowrap;
      }
      .cfg__actions button {
        padding: 3px 8px;
        border-radius: 4px;
        border: 1px solid var(--border-color, #d1d5db);
        background: transparent;
        color: inherit;
        font-size: 11px;
        cursor: pointer;
      }
      .cfg__save {
        border-color: var(--color-primary, #2563eb) !important;
        color: var(--color-primary, #2563eb);
      }
      .cfg__empty {
        padding: 12px;
        color: var(--text-secondary);
        font-size: 13px;
      }
      .sr-only {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip: rect(0 0 0 0);
      }
      @media (max-width: 900px) {
        .cfg__body {
          grid-template-columns: 1fr;
        }
        .cfg__list {
          max-height: 220px;
        }
      }
    `,
  ],
})
export class ConfigurationCenterPageComponent {
  protected readonly admin = inject(ConfigurationAdminService);
  private readonly route = inject(ActivatedRoute);

  /** Selected domain, from the URL so a deep link and the back button both work. */
  protected readonly activeCode = toSignal(
    this.route.queryParamMap.pipe(map((params) => params.get('category'))),
    { initialValue: null },
  );

  protected readonly activeCategory = computed(
    () => this.admin.categories().find((c) => c.code === this.activeCode()) ?? null,
  );

  protected readonly values = computed(() => {
    const code = this.activeCode();
    return code ? this.admin.valuesOf(code)() : [];
  });

  /** Value currently open for editing; only one row at a time. */
  protected readonly editing = signal<string | null>(null);

  protected draftLabel = '';
  protected draftSortOrder = 0;
  protected draftAttributes = '{}';

  /**
   * Why the attribute JSON cannot be saved, or null when it can.
   *
   * Validated as the administrator types rather than on submit, because a rejected save that has
   * already left the browser is a worse experience than a disabled button — and for the workflow
   * categories the attributes *are* the rules, so a malformed payload is a broken lifecycle.
   */
  protected readonly attributeError = computed(() => {
    const raw = this.draftAttributesSignal();
    if (raw.trim() === '') return null;
    try {
      const parsed: unknown = JSON.parse(raw);
      if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
        return 'Attributes must be a JSON object.';
      }
      return null;
    } catch {
      return 'Attributes are not valid JSON.';
    }
  });

  /** Mirrors the textarea into a signal so validation is reactive. */
  private readonly draftAttributesSignal = signal('{}');

  protected edit(value: ConfigurationValueDescriptor): void {
    this.editing.set(value.id);
    this.draftLabel = value.label;
    this.draftSortOrder = value.sortOrder;
    // Attributes and compound values are one JSON document to the administrator; the split into
    // scalars and structures is a detail of how the client reads them, not of how they are stored.
    const metadata = { ...value.attributes, ...value.compound };
    this.draftAttributes = JSON.stringify(metadata, null, 2);
    this.draftAttributesSignal.set(this.draftAttributes);
  }

  protected cancel(): void {
    this.editing.set(null);
  }

  protected commit(categoryCode: string, value: ConfigurationValueDescriptor): void {
    this.draftAttributesSignal.set(this.draftAttributes);
    if (this.attributeError()) return;

    this.admin.save(categoryCode, {
      code: value.id,
      name: this.draftLabel,
      sortOrder: Number(this.draftSortOrder),
      metadata: this.draftAttributes.trim() === '' ? undefined : this.draftAttributes,
    });
    this.editing.set(null);
  }

  protected restore(categoryCode: string, valueCode: string): void {
    this.admin.restore(categoryCode, valueCode);
    if (this.editing() === valueCode) this.editing.set(null);
  }

  /** Compact read-only rendering of a value's attributes for the table. */
  protected describeAttributes(value: ConfigurationValueDescriptor): string {
    const merged: Record<string, unknown> = { ...value.attributes, ...value.compound };
    const keys = Object.keys(merged);
    if (keys.length === 0) return '—';

    return keys
      .map((key) => {
        const raw = merged[key];
        // Rules are trees; showing them inline would fill the column. The shape is enough to
        // recognise, and Edit shows the whole document.
        const rendered = typeof raw === 'object' && raw !== null ? '{…}' : String(raw);
        return `${key}: ${rendered}`;
      })
      .join(' · ');
  }

  /** Keeps validation in step with the textarea without a form library. */
  protected onAttributesChanged(raw: string): void {
    this.draftAttributes = raw;
    this.draftAttributesSignal.set(raw);
  }
}
