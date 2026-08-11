import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Workspace layout primitives — Stage 1.3.
 *
 * Audit's fifteen pages each invented their own container padding: nine
 * distinct values across the module, from 8px/12px to 24px, all hardcoded
 * despite `--page-padding` already existing. These components make the standard
 * the path of least resistance so that cannot recur.
 *
 * Platform-level, not Audit-level — every future workspace composes the same
 * primitives (Step 15).
 *
 * Structural only. They own spacing, rhythm and heading semantics; they never
 * fetch, format or decide anything about the content placed inside them.
 *
 * Styles live in sibling stylesheets rather than inline `styles` arrays, which
 * is the pattern the rest of this codebase uses for multi-component files.
 */

/**
 * Standard page container.
 *
 * The single source of horizontal padding and vertical rhythm for a workspace
 * page. `--page-padding` is the token the rest of the product already uses, so
 * an Audit page adopting this lands on the same grid as Journey or Compliance
 * without further coordination.
 */
@Component({
  selector: 'app-workspace-page',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<div class="ws-page" [class.is-wide]="wide()"><ng-content /></div>`,
  styleUrl: './workspace-page.component.scss',
})
export class WorkspacePageComponent {
  /** Opt out of the reading-width cap for data-dense grids. */
  readonly wide = input(false);
}

/**
 * Standard page header: title, description and trailing actions.
 *
 * Replaces the per-page `.page-header` blocks, which differed in heading size,
 * subtitle colour and action alignment. Actions project into the trailing slot
 * so every page puts them in the same place.
 */
@Component({
  selector: 'app-workspace-header',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="ws-header">
      <div class="ws-header__text">
        <h1>{{ title() }}</h1>
        @if (description()) {
          <p class="ws-header__desc">{{ description() }}</p>
        }
      </div>
      <div class="ws-header__actions">
        <ng-content select="[headerActions]" />
      </div>
    </header>
  `,
  styleUrl: './workspace-header.component.scss',
})
export class WorkspaceHeaderComponent {
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
}

/**
 * A titled content section within a page.
 *
 * Gives every panel the same surface treatment and heading rhythm, so a card on
 * Planning and a card on Findings read as the same kind of object.
 */
@Component({
  selector: 'app-workspace-section',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="ws-section" [class.is-plain]="plain()">
      @if (heading()) {
        <div class="ws-section__head">
          <h2>{{ heading() }}</h2>
          <ng-content select="[sectionActions]" />
        </div>
      }
      <ng-content />
    </section>
  `,
  styleUrl: './workspace-section.component.scss',
})
export class WorkspaceSectionComponent {
  readonly heading = input<string | null>(null);
  /** Drop the card surface — for sections that host their own bordered table. */
  readonly plain = input(false);
}
