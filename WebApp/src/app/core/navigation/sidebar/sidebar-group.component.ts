import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { IsActiveMatchOptions } from '@angular/router';
import { SidebarItemComponent } from './sidebar-item.component';
import type { BadgeVariant, SidebarMenuItem, SidebarMenuSection } from './sidebar.models';

@Component({
  selector: 'app-sidebar-group',
  standalone: true,
  imports: [SidebarItemComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="menu-section" [attr.aria-labelledby]="'sidebar-section-' + section().id">
      <div class="section-header">
        <h2 class="section-label" [id]="'sidebar-section-' + section().id">
          {{ section().title }}
        </h2>
      </div>
      <div class="section-divider" aria-hidden="true"></div>
      <div class="section-items">
        @for (item of section().items; track item.id ?? item.route) {
          <app-sidebar-item
            [item]="item"
            [expanded]="expandedLabel() === item.label"
            [tooltipDisabled]="tooltipDisabled()"
            [journeyActive]="journeyActive()"
            [badge]="badgeFor(item)"
            [exactActiveLinkOptions]="exactActiveLinkOptions()"
            [subsetActiveLinkOptions]="subsetActiveLinkOptions()"
            [disabled]="disabledFor(item)"
            [disabledReason]="reasonFor(item)"
            [isChildDisabled]="childDisabledFn"
            [childReason]="childReasonFn"
            (toggleExpand)="toggleExpand.emit(item.label)"
          />
        }
      </div>
    </section>
  `,
  styles: `
    :host {
      display: block;
    }

    .menu-section {
      display: flex;
      flex-direction: column;
      margin-bottom: 0;
      padding: 0 0 var(--sidebar-section-gap, 12px);
      border-bottom: 1px solid var(--color-border-subtle);
    }

    :host:last-child .menu-section {
      border-bottom: none;
      padding-bottom: 0;
    }

    .section-header {
      padding: 0 var(--sidebar-item-pad-x, 12px);
      min-height: 18px;
      margin-bottom: 4px;
    }

    .section-label {
      margin: 0;
      font-size: var(--sidebar-section-header-size, 11px);
      font-weight: 600;
      letter-spacing: 0.08em;
      text-transform: uppercase;
      color: var(--color-text-muted);
      line-height: 1.2;
    }

    .section-divider {
      display: none;
    }

    .section-items {
      display: flex;
      flex-direction: column;
      gap: 1px;
      padding: 0 4px;
    }
  `,
})
export class SidebarGroupComponent {
  readonly section = input.required<SidebarMenuSection>();
  readonly expandedLabel = input<string | null>(null);
  readonly tooltipDisabled = input(true);
  readonly journeyActive = input(false);
  readonly exactActiveLinkOptions = input.required<IsActiveMatchOptions>();
  readonly subsetActiveLinkOptions = input.required<IsActiveMatchOptions>();
  readonly resolveBadge =
    input.required<(item: SidebarMenuItem) => { count: number; variant: BadgeVariant } | null>();
  readonly isItemDisabled = input.required<(item: SidebarMenuItem) => boolean>();
  readonly itemDisabledReason = input.required<(item: SidebarMenuItem) => string>();
  readonly toggleExpand = output<string>();

  readonly childDisabledFn = (item: SidebarMenuItem): boolean => this.isItemDisabled()(item);
  readonly childReasonFn = (item: SidebarMenuItem): string => this.itemDisabledReason()(item);

  badgeFor(item: SidebarMenuItem): { count: number; variant: BadgeVariant } | null {
    return this.resolveBadge()(item);
  }

  disabledFor(item: SidebarMenuItem): boolean {
    return this.isItemDisabled()(item);
  }

  reasonFor(item: SidebarMenuItem): string {
    return this.itemDisabledReason()(item);
  }
}
