import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WorkspacePreferencesService, type NavDensity } from './workspace-preferences.service';

@Component({
  selector: 'app-navigation-preferences',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="nav-prefs" aria-labelledby="nav-prefs-title">
      <h2 id="nav-prefs-title" class="nav-prefs__title">Navigation preferences</h2>
      <p class="nav-prefs__lead">
        Personalize sidebar density, sections, and landing page. Saved on this device.
      </p>

      <div class="nav-prefs__grid">
        <label class="nav-prefs__field">
          <span>Sidebar density</span>
          <select [ngModel]="prefs.prefs().density" (ngModelChange)="setDensity($event)">
            <option value="comfortable">Comfortable</option>
            <option value="compact">Compact</option>
          </select>
        </label>

        <label class="nav-prefs__field">
          <span>Default landing page</span>
          <input
            type="text"
            [ngModel]="prefs.prefs().preferredLandingPage"
            (ngModelChange)="prefs.update({ preferredLandingPage: $event })"
          />
        </label>

        <label class="nav-prefs__field">
          <span>Max recent items</span>
          <input
            type="number"
            min="5"
            max="20"
            [ngModel]="prefs.prefs().maxRecentItems"
            (ngModelChange)="prefs.update({ maxRecentItems: +$event || 10 })"
          />
        </label>
      </div>

      <div class="nav-prefs__toggles">
        <label
          ><input
            type="checkbox"
            [ngModel]="prefs.prefs().showFavorites"
            (ngModelChange)="prefs.update({ showFavorites: $event })"
          />
          Show Favorites</label
        >
        <label
          ><input
            type="checkbox"
            [ngModel]="prefs.prefs().showRecent"
            (ngModelChange)="prefs.update({ showRecent: $event })"
          />
          Show Recent</label
        >
        <label
          ><input
            type="checkbox"
            [ngModel]="prefs.prefs().showPinned"
            (ngModelChange)="prefs.update({ showPinned: $event })"
          />
          Show Pinned</label
        >
        <label
          ><input
            type="checkbox"
            [ngModel]="prefs.prefs().showBadges"
            (ngModelChange)="prefs.update({ showBadges: $event })"
          />
          Show badges</label
        >
        <label
          ><input
            type="checkbox"
            [ngModel]="prefs.prefs().animations"
            (ngModelChange)="prefs.update({ animations: $event })"
          />
          Navigation animations</label
        >
        <label
          ><input
            type="checkbox"
            [ngModel]="prefs.prefs().compactMode"
            (ngModelChange)="prefs.update({ compactMode: $event })"
          />
          Compact mode</label
        >
        <label
          ><input
            type="checkbox"
            [ngModel]="prefs.prefs().defaultCollapsedSidebar"
            (ngModelChange)="prefs.update({ defaultCollapsedSidebar: $event })"
          />
          Start with collapsed sidebar</label
        >
      </div>

      <button type="button" class="nav-prefs__reset" (click)="prefs.reset()">
        Reset to defaults
      </button>
    </section>
  `,
  styles: `
    .nav-prefs {
      display: flex;
      flex-direction: column;
      gap: 16px;
      padding: 8px 0;
    }
    .nav-prefs__title {
      margin: 0;
      font-size: 18px;
      font-weight: 650;
    }
    .nav-prefs__lead {
      margin: 0;
      font-size: 13px;
      color: var(--color-text-secondary, #667085);
    }
    .nav-prefs__grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 12px;
    }
    .nav-prefs__field {
      display: flex;
      flex-direction: column;
      gap: 6px;
      font-size: 13px;
      font-weight: 600;
    }
    .nav-prefs__field input,
    .nav-prefs__field select {
      font-weight: 400;
      padding: 8px 10px;
      border-radius: 8px;
      border: 1px solid var(--color-border-subtle, #e5e7eb);
      background: var(--color-bg-page, #fff);
    }
    .nav-prefs__toggles {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 10px;
      font-size: 13px;
    }
    .nav-prefs__toggles label {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .nav-prefs__reset {
      align-self: flex-start;
      border: 1px solid var(--color-border-subtle, #e5e7eb);
      background: transparent;
      border-radius: 8px;
      padding: 8px 12px;
      cursor: pointer;
      font-size: 13px;
    }
  `,
})
export class NavigationPreferencesComponent {
  readonly prefs = inject(WorkspacePreferencesService);

  setDensity(density: NavDensity): void {
    this.prefs.update({ density, compactMode: density === 'compact' });
  }
}
