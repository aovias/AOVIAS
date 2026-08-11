import { Injectable, computed, inject } from '@angular/core';
import { NavigationQueryService } from '../engine/navigation-query.service';
import type { SidebarViewSection } from '../models/navigation.model';

@Injectable({ providedIn: 'root' })
export class SidebarProvider {
  private readonly query = inject(NavigationQueryService);

  readonly sections = computed((): readonly SidebarViewSection[] =>
    this.query.sidebarViewSections(),
  );
}
