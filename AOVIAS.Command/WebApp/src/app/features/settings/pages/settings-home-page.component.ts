import { ChangeDetectionStrategy, Component } from '@angular/core';

import { RouterLink } from '@angular/router';

import { MatCardModule } from '@angular/material/card';

import { MatIconModule } from '@angular/material/icon';

import { getSettingsHomeCards } from '../models/settings-nav.model';

@Component({
  selector: 'app-settings-home-page',

  standalone: true,

  imports: [RouterLink, MatCardModule, MatIconModule],

  changeDetection: ChangeDetectionStrategy.OnPush,

  template: `
    <div class="settings-home">
      <header class="settings-home__hero">
        <p class="settings-home__eyebrow">Settings</p>

        <h1 class="settings-home__title">Administration Center</h1>

        <p class="settings-home__subtitle">
          Manage platform configuration, identity, workforce, AI services, and enterprise
          integrations.
        </p>
      </header>

      <div class="settings-home__grid">
        @for (card of cards; track card.route) {
          <a
            [routerLink]="card.route"
            class="settings-home__card-link"
            [attr.aria-label]="card.title"
          >
            <mat-card
              class="settings-home__card"
              [class.settings-home__card--soon]="card.status === 'coming-soon'"
            >
              <div class="settings-home__card-icon-wrap">
                <mat-icon aria-hidden="true">{{ card.icon }}</mat-icon>
              </div>

              <h2 class="settings-home__card-title">{{ card.title }}</h2>

              <p class="settings-home__card-description">{{ card.description }}</p>

              <footer class="settings-home__card-footer" [attr.data-status]="card.status">
                @if (card.status === 'coming-soon') {
                  <mat-icon aria-hidden="true">schedule</mat-icon>
                }

                <span>{{ card.footer }}</span>
              </footer>
            </mat-card>
          </a>
        }
      </div>
    </div>
  `,

  styles: [
    `
      .settings-home__hero {
        margin-bottom: 32px;

        animation: settingsFadeIn 0.28s ease;
      }

      .settings-home__eyebrow {
        margin: 0 0 6px;

        font-size: 12px;

        font-weight: 700;

        letter-spacing: 0.08em;

        text-transform: uppercase;

        color: var(--color-brand-primary, var(--color-primary));
      }

      .settings-home__title {
        margin: 0 0 10px;

        font-size: var(--font-size-page-title);

        font-weight: 600;

        letter-spacing: -0.02em;

        line-height: 1.15;

        color: var(--color-text-primary, #111827);
      }

      .settings-home__subtitle {
        margin: 0;

        max-width: 62ch;

        font-size: var(--font-size-heading-sm);

        line-height: 1.55;

        color: var(--color-text-secondary, #6b7280);
      }

      .settings-home__grid {
        display: grid;

        grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));

        gap: 20px;
      }

      .settings-home__card-link {
        text-decoration: none;

        color: inherit;

        display: block;

        height: 100%;
      }

      .settings-home__card {
        height: 100%;

        display: flex;

        flex-direction: column;

        padding: 24px;

        border-radius: 16px;

        border: 1px solid var(--color-border-subtle, #e5e7eb);

        box-shadow: 0 1px 2px rgba(15, 23, 42, 0.04);

        transition:
          box-shadow 0.22s ease,
          transform 0.22s ease,
          border-color 0.22s ease;
      }

      .settings-home__card-link:hover .settings-home__card,
      .settings-home__card-link:focus-visible .settings-home__card {
        box-shadow: 0 12px 32px rgba(15, 23, 42, 0.1);

        transform: translateY(-3px);

        border-color: color-mix(
          in srgb,
          var(--color-brand-primary, var(--color-primary)) 24%,
          #e5e7eb
        );
      }

      .settings-home__card-link:focus-visible {
        outline: none;
      }

      .settings-home__card-link:focus-visible .settings-home__card {
        outline: 2px solid var(--color-brand-primary, var(--color-primary));

        outline-offset: 2px;
      }

      .settings-home__card--soon {
        opacity: 0.92;
      }

      .settings-home__card-icon-wrap {
        width: 52px;

        height: 52px;

        border-radius: 14px;

        display: inline-flex;

        align-items: center;

        justify-content: center;

        margin-bottom: 18px;

        background: color-mix(
          in srgb,
          var(--color-brand-primary, var(--color-primary)) 12%,
          transparent
        );
      }

      .settings-home__card-icon-wrap mat-icon {
        color: var(--color-brand-primary, var(--color-primary));

        font-size: 28px;

        width: 28px;

        height: 28px;
      }

      .settings-home__card-title {
        margin: 0 0 10px;

        font-size: 18px;

        font-weight: 600;

        letter-spacing: -0.01em;
      }

      .settings-home__card-description {
        margin: 0;

        flex: 1;

        font-size: 14px;

        line-height: 1.55;

        color: var(--color-text-secondary, #6b7280);
      }

      .settings-home__card-footer {
        display: inline-flex;

        align-items: center;

        gap: 6px;

        margin-top: 20px;

        padding-top: 16px;

        border-top: 1px solid var(--color-border-subtle, #e5e7eb);

        font-size: 12px;

        font-weight: 700;

        letter-spacing: 0.03em;

        text-transform: uppercase;

        color: var(--color-text-secondary, #9ca3af);
      }

      .settings-home__card-footer[data-status='configured'] {
        color: #047857;
      }

      .settings-home__card-footer[data-status='coming-soon'] {
        color: var(--color-text-secondary, #9ca3af);
      }

      .settings-home__card-footer mat-icon {
        font-size: 16px;

        width: 16px;

        height: 16px;
      }

      @keyframes settingsFadeIn {
        from {
          opacity: 0;

          transform: translateY(4px);
        }

        to {
          opacity: 1;

          transform: translateY(0);
        }
      }

      @media (max-width: 640px) {
        .settings-home__title {
          font-size: var(--font-size-page-title-compact);
        }

        .settings-home__grid {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class SettingsHomePageComponent {
  readonly cards = getSettingsHomeCards();
}
